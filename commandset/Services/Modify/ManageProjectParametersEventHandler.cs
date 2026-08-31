using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class ManageProjectParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public string Action { get; private set; }
        public string SharedParamFile { get; private set; }
        public string ParamGroup { get; private set; }
        public JArray Params { get; private set; }
        public AIResult<object> Result { get; private set; }

        public void SetParameters(string action, string sharedParamFile, string paramGroup, JArray paramList)
        {
            Action = action;
            SharedParamFile = sharedParamFile;
            ParamGroup = paramGroup;
            Params = paramList;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                switch (Action.ToLower())
                {
                    case "list":
                        Result = ListProjectParameters();
                        break;

                    case "add":
                        Result = AddSharedParameters();
                        break;

                    default:
                        throw new ArgumentException($"Unsupported action: {Action}. Supported: list, add");
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<object> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private AIResult<object> ListProjectParameters()
        {
            var bindingMap = Doc.ParameterBindings;
            var iterator = bindingMap.ForwardIterator();
            var parameters = new List<object>();
            while (iterator.MoveNext())
            {
                var def = iterator.Key;
                var binding = iterator.Current as ElementBinding;
                var categories = binding?.Categories.Cast<Category>().Select(c => c.Name).ToList();
                parameters.Add(new
                {
                    Name = def.Name,
#if REVIT2023_OR_GREATER
                    ParameterType = def.GetDataType().ToString(),
                    Group = "PG_DATA",
                    Visible = true,
#elif REVIT2025_OR_GREATER
                    ParameterType = def.ParameterType.ToString(),
                    Group = def.ParameterGroup.ToString(),
                    Visible = def.Visible,
#else
                    ParameterType = def.ParameterType.ToString(),
                    Group = def.ParameterGroup.ToString(),
                    Visible = true,
#endif
                    Categories = categories
                });
            }
            return new AIResult<object> { Success = true, Response = parameters };
        }

        private AIResult<object> AddSharedParameters()
        {
            if (string.IsNullOrEmpty(SharedParamFile))
                throw new ArgumentException("sharedParamFile is required for add action");
            if (Params == null || Params.Count == 0)
                throw new ArgumentException("params array is required for add action");

            var app = uiApp.Application;
            var sharedParamFile = app.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                sharedParamFile = app.OpenSharedParameterFile();
                if (sharedParamFile == null)
                    throw new Exception("Could not open shared parameter file. Ensure the path is configured in Revit options.");
            }

            var group = sharedParamFile.Groups.get_Item(ParamGroup ?? "General");
            if (group == null)
                throw new ArgumentException($"Shared parameter group '{ParamGroup}' not found in file");

            var bindingMap = Doc.ParameterBindings;
            using (var trans = new Transaction(Doc, "Add Project Parameters"))
            {
                trans.Start();
                foreach (var item in Params)
                {
                    var paramObj = item as JObject;
                    if (paramObj == null) continue;

                    string paramName = paramObj["name"]?.Value<string>();
                    var categoryNames = paramObj["categories"]?.ToObject<List<string>>();

                    if (string.IsNullOrEmpty(paramName)) continue;

                    var sharedParam = group.Definitions.get_Item(paramName);
                    if (sharedParam == null)
                        throw new ArgumentException($"Shared parameter '{paramName}' not found in group '{ParamGroup}'");

                    var newBinding = app.Create.NewInstanceBinding();
                    if (categoryNames != null && categoryNames.Count > 0)
                    {
                        var catSet = new CategorySet();
                        foreach (var catName in categoryNames)
                        {
#if REVIT2026_OR_GREATER
                            // R26: Category.GetCategory(Document, ElementId)
                            Category cat = null;
                            var allCats = Doc.Settings.Categories;
                            foreach (Category c in allCats)
                            {
                                if (c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase))
                                {
                                    cat = c;
                                    break;
                                }
                            }
#elif REVIT2025_OR_GREATER
                            Category cat = Category.GetCategory(Doc, catName);
#else
                            Category cat = null;
                            var allCats = Doc.Settings.Categories;
                            foreach (Category c in allCats)
                            {
                                if (c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase))
                                {
                                    cat = c;
                                    break;
                                }
                            }
#endif
                            if (cat != null)
                                catSet.Insert(cat);
                        }
                        newBinding.Categories = catSet;
                    }
                    else
                    {
                        var catSet = new CategorySet();
                        catSet.Insert(Category.GetCategory(Doc, BuiltInCategory.OST_GenericModel));
                        newBinding.Categories = catSet;
                    }

#if REVIT2026_OR_GREATER
                    // R26: BuiltInParameterGroup removed, use ForgeTypeId
                    bindingMap.Insert(sharedParam, newBinding);
#elif REVIT2025_OR_GREATER
                    bindingMap.Insert(sharedParam, newBinding, BuiltInParameterGroup.PG_DATA);
#else
                    // R20-R25: BuiltInParameterGroup
                    bindingMap.Insert(sharedParam, newBinding, BuiltInParameterGroup.PG_DATA);
#endif
                }
                trans.Commit();
            }
            return new AIResult<object> { Success = true, Response = true };
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Manage Project Parameters";
    }
}
