using RevitMCPCommandSet.Models.Annotation;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Annotation
{
    public class CreateTagEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<TagCreationInfo> CreatedInfo { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<TagCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var data in CreatedInfo)
                {
                    View view = null;
                    if (data.ViewId != -1 && data.ViewId != 0)
                    {
                        view = doc.GetElement(new ElementId(data.ViewId)) as View;
                    }

                    if (view == null)
                    {
                        view = doc.ActiveView;
                    }

                    Element targetElement = doc.GetElement(new ElementId(data.ElementId));
                    if (targetElement == null)
                    {
                        _warnings.Add($"Element with ID {data.ElementId} not found.");
                        continue;
                    }

                    TagOrientation orientation = data.Orientation == 0
                        ? TagOrientation.Horizontal
                        : TagOrientation.Vertical;

                    XYZ location = JZPoint.ToXYZ(data.Location);

                    FamilySymbol tagType = null;
                    if (data.TagTypeId != -1 && data.TagTypeId != 0)
                    {
                        Element typeElem = doc.GetElement(new ElementId(data.TagTypeId));
                        if (typeElem != null && typeElem is FamilySymbol)
                        {
                            tagType = typeElem as FamilySymbol;
                        }
                    }

                    if (tagType == null)
                    {
                        BuiltInCategory tagCategory = BuiltInCategory.OST_MultiCategoryTags;

                        if (!string.IsNullOrEmpty(data.TagCategory))
                        {
                            tagCategory = GetTagCategory(data.TagCategory);
                        }

                        tagType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilySymbol))
                            .WhereElementIsElementType()
                            .Where(e => e.Category != null &&
                                   e.Category.Id.GetIntValue() == (int)tagCategory)
                            .Cast<FamilySymbol>()
                            .FirstOrDefault();

                        if (tagType == null && tagCategory != BuiltInCategory.OST_MultiCategoryTags)
                        {
                            tagType = new FilteredElementCollector(doc)
                                .OfClass(typeof(FamilySymbol))
                                .WhereElementIsElementType()
                                .Where(e => e.Category != null &&
                                       e.Category.Id.GetIntValue() == (int)BuiltInCategory.OST_MultiCategoryTags)
                                .Cast<FamilySymbol>()
                                .FirstOrDefault();
                        }

                        if (data.TagTypeId != -1 && data.TagTypeId != 0)
                        {
                            _warnings.Add($"Requested tag typeId {data.TagTypeId} not found. Defaulted to '{tagType?.Name}' (ID: {tagType?.Id.GetIntValue()})");
                        }
                    }

                    if (tagType == null)
                    {
                        _warnings.Add("No suitable tag type found in project.");
                        continue;
                    }

                    using (Transaction trans = new Transaction(doc, "创建标记"))
                    {
                        trans.Start();

                        if (!tagType.IsActive)
                        {
                            tagType.Activate();
                            doc.Regenerate();
                        }

                        IndependentTag tag = IndependentTag.Create(
                            doc,
                            tagType.Id,
                            view.Id,
                            new Reference(targetElement),
                            data.HasLeader,
                            orientation,
                            location);

                        if (tag != null)
                        {
                            elementIds.Add(tag.Id.GetIntValue());
                        }

                        trans.Commit();
                    }
                }

                string message = $"Successfully created {elementIds.Count} tag(s).";
                if (_warnings.Count > 0)
                {
                    message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = elementIds,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"创建标记时出错: {ex.Message}",
                };
                TaskDialog.Show("错误", $"创建标记时出错: {ex.Message}");
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create Tag";
        }

        private BuiltInCategory GetTagCategory(string categoryName)
        {
            switch (categoryName.ToLower())
            {
                case "door":
                    return BuiltInCategory.OST_DoorTags;
                case "window":
                    return BuiltInCategory.OST_WindowTags;
                case "wall":
                    return BuiltInCategory.OST_WallTags;
                case "room":
                    return BuiltInCategory.OST_RoomTags;
                case "multi":
                case "multicategory":
                    return BuiltInCategory.OST_MultiCategoryTags;
                default:
                    return BuiltInCategory.OST_MultiCategoryTags;
            }
        }
    }
}
