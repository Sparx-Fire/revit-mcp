using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class ManageFamilyParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public string Action { get; private set; }
        public int FamilyId { get; private set; }
        public string Name { get; private set; }
        public string NewName { get; private set; }
        public string Formula { get; private set; }
        public string ParamType { get; private set; }
        public AIResult<bool> Result { get; private set; }

        public void SetParameters(string action, int familyId, string name, string newName, string formula, string paramType)
        {
            Action = action;
            FamilyId = familyId;
            Name = name;
            NewName = newName;
            Formula = formula;
            ParamType = paramType;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                var family = Doc.GetElement(new ElementId(FamilyId)) as Family;
                if (family == null)
                {
                    Result = new AIResult<bool> { Success = false, Message = $"Family {FamilyId} not found" };
                    return;
                }
                var familyManager = Doc.FamilyManager;
                if (familyManager == null)
                {
                    Result = new AIResult<bool> { Success = false, Message = "Family document not open or FamilyManager unavailable" };
                    return;
                }
                using (var trans = new Transaction(Doc, $"Manage Family Parameters - {Action}"))
                {
                    trans.Start();
                    switch (Action.ToLower())
                    {
                        case "add":
                            if (string.IsNullOrEmpty(Name))
                                throw new ArgumentException("name is required for add action");
#if REVIT2026_OR_GREATER
                            // R26: AddParameter takes (ExternalDefinition, ForgeTypeId, bool)
                            throw new NotSupportedException("Family parameter addition not supported in Revit 2026 via this API");
#elif REVIT2025_OR_GREATER
                            var paramTypeEnum = ForgeTypeId.GetForgeTypeId(ParamType ?? "IFC_TYPE");
                            familyManager.AddParameter(Name, paramTypeEnum);
#else
                            // R20-R22: AddParameter takes (ExternalDefinition, BuiltInParameterGroup, bool)
                            throw new NotSupportedException("Family parameter addition not supported in Revit 2020-2022 via this API");
#endif
                            break;

                        case "rename":
                            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(NewName))
                                throw new ArgumentException("name and newName are required for rename action");
                            var fp = familyManager.get_Parameter(Name);
                            if (fp == null)
                                throw new ArgumentException($"Parameter '{Name}' not found in family");
                            familyManager.RenameParameter(fp, NewName);
                            break;

                        case "remove":
                            if (string.IsNullOrEmpty(Name))
                                throw new ArgumentException("name is required for remove action");
                            var fpRemove = familyManager.get_Parameter(Name);
                            if (fpRemove == null)
                                throw new ArgumentException($"Parameter '{Name}' not found in family");
                            familyManager.RemoveParameter(fpRemove);
                            break;

                        case "set_formula":
                            if (string.IsNullOrEmpty(Name))
                                throw new ArgumentException("name is required for set_formula action");
                            var fpFormula = familyManager.get_Parameter(Name);
                            if (fpFormula == null)
                                throw new ArgumentException($"Parameter '{Name}' not found in family");
                            familyManager.SetFormula(fpFormula, Formula ?? "");
                            break;

                        default:
                            throw new ArgumentException($"Unsupported action: {Action}");
                    }
                    trans.Commit();
                }
                Result = new AIResult<bool> { Success = true, Response = true };
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Manage Family Parameters";
    }
}
