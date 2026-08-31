using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateScheduleEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ScheduleCreationInfo> CreatedInfo { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<ScheduleCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var scheduleIds = new List<int>();
                _warnings.Clear();

                foreach (var info in CreatedInfo)
                {
                    using (Transaction trans = new Transaction(doc, "Create Schedule"))
                    {
                        trans.Start();

                        ViewSchedule schedule = null;
                        string scheduleType = info.Type?.ToLowerInvariant() ?? "regular";

                        switch (scheduleType)
                        {
                            case "regular":
                            case "常规":
                                schedule = CreateRegularSchedule(info);
                                break;
                            case "material":
                            case "materialtakeoff":
                            case "材料":
                                schedule = CreateMaterialTakeoff(info);
                                break;
                            case "keynote":
                            case "key":
                            case "钥匙":
                                schedule = CreateKeySchedule(info);
                                break;
                            case "viewlist":
                            case "视图列表":
                                schedule = CreateBuiltInSchedule(BuiltInCategory.OST_Views, "View List");
                                break;
                            case "sheetlist":
                            case "图纸列表":
                                schedule = CreateBuiltInSchedule(BuiltInCategory.OST_Sheets, "Sheet List");
                                break;
                            case "revision":
                            case "修订":
                                schedule = CreateBuiltInSchedule(BuiltInCategory.OST_Revisions, "Revision Schedule");
                                break;
                            default:
                                schedule = CreateRegularSchedule(info);
                                break;
                        }

                        if (schedule != null)
                        {
                            if (!string.IsNullOrEmpty(info.Name))
                            {
                                schedule.Name = info.Name;
                            }

                            if (info.ShowTitle.HasValue)
                            {
#if REVIT2026_OR_GREATER
                                Parameter titleParam = schedule.LookupParameter("Show Title");
                                titleParam?.Set(info.ShowTitle.Value ? 1 : 0);
#elif REVIT2025_OR_GREATER
                                schedule.get_Parameter(BuiltInParameter.VIEW_TITLE_VISIBLE)?.Set(info.ShowTitle.Value ? 1 : 0);
#endif
                            }

                            if (info.ShowHeaders.HasValue)
                            {
#if REVIT2026_OR_GREATER
                                Parameter p = schedule.LookupParameter("Show Headers");
                                p?.Set(info.ShowHeaders.Value ? 1 : 0);
#elif REVIT2025_OR_GREATER
                                schedule.ShowHeaders = info.ShowHeaders.Value;
#elif REVIT2025_OR_GREATER
                                schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_HEADER)?.Set(info.ShowHeaders.Value ? 1 : 0);
#endif
                            }

                            if (info.ShowGridLines.HasValue)
                            {
#if REVIT2026_OR_GREATER
                                Parameter p = schedule.LookupParameter("Show Grid Lines");
                                p?.Set(info.ShowGridLines.Value ? 1 : 0);
#elif REVIT2025_OR_GREATER
                                schedule.ShowGridLines = info.ShowGridLines.Value;
#elif REVIT2025_OR_GREATER
                                schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_GRID_LINES)?.Set(info.ShowGridLines.Value ? 1 : 0);
#endif
                            }

                            if (info.ShowOutlines.HasValue)
                            {
#if REVIT2026_OR_GREATER
                                Parameter p = schedule.LookupParameter("Show Outlines");
                                p?.Set(info.ShowOutlines.Value ? 1 : 0);
#elif REVIT2025_OR_GREATER
                                schedule.ShowOutlines = info.ShowOutlines.Value;
#elif REVIT2025_OR_GREATER
                                schedule.get_Parameter(BuiltInParameter.VIEW_SCHEDULE_SHOW_OUTLINES)?.Set(info.ShowOutlines.Value ? 1 : 0);
#endif
                            }

                            if (!string.IsNullOrEmpty(info.TemplateId) && int.TryParse(info.TemplateId, out int templateIntId))
                            {
                                ElementId templateId = new ElementId(templateIntId);
                                View templateView = doc.GetElement(templateId) as View;
                                if (templateView != null && templateView.IsTemplate)
                                {
                                    schedule.ViewTemplateId = templateId;
                                }
                            }

                            foreach (var param in info.Parameters)
                            {
                                Parameter schedParam = schedule.LookupParameter(param.Key);
                                if (schedParam != null)
                                {
                                    SetParameterValue(schedParam, param.Value);
                                }
                            }

                            scheduleIds.Add(schedule.Id.GetIntValue());
                        }

                        trans.Commit();
                    }
                }

                string message = $"Successfully created {scheduleIds.Count} schedule(s).";
                if (_warnings.Count > 0)
                {
                    message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = scheduleIds,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error creating schedule: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private ViewSchedule CreateRegularSchedule(ScheduleCreationInfo info)
        {
            ElementId categoryId = FindCategoryId(info);
            if (categoryId == ElementId.InvalidElementId)
            {
                _warnings.Add($"Could not resolve category for schedule. Schedule not created.");
                return null;
            }

            ViewSchedule schedule = ViewSchedule.CreateSchedule(doc, categoryId);
            return schedule;
        }

        private ViewSchedule CreateMaterialTakeoff(ScheduleCreationInfo info)
        {
            ElementId categoryId = FindCategoryId(info);
            if (categoryId == ElementId.InvalidElementId) return null;

            ViewSchedule schedule = ViewSchedule.CreateMaterialTakeoff(doc, categoryId);
            return schedule;
        }

        private ViewSchedule CreateKeySchedule(ScheduleCreationInfo info)
        {
            ElementId categoryId = FindCategoryId(info);
            if (categoryId == ElementId.InvalidElementId) return null;

            ViewSchedule schedule = ViewSchedule.CreateKeySchedule(doc, categoryId);
            return schedule;
        }

        private ViewSchedule CreateBuiltInSchedule(BuiltInCategory category, string defaultName)
        {
            ElementId catId = new ElementId((int)category);
            ViewSchedule schedule = ViewSchedule.CreateSchedule(doc, catId);
            if (schedule != null && !string.IsNullOrEmpty(defaultName))
            {
                try { schedule.Name = defaultName; } catch { }
            }
            return schedule;
        }

        private ElementId FindCategoryId(ScheduleCreationInfo info)
        {
            if (info.CategoryId > 0)
            {
                ElementId catId = new ElementId(info.CategoryId);
                Category cat = Category.GetCategory(doc, catId);
                if (cat != null) return catId;
            }

            if (!string.IsNullOrEmpty(info.CategoryName))
            {
                BuiltInCategory bic;
                string catName = info.CategoryName.Replace(" ", "").Replace("-", "");
                if (Enum.TryParse(catName, true, out bic))
                {
                    return new ElementId((int)bic);
                }

                Category matchedCat = null;
                foreach (Category c in doc.Settings.Categories)
                {
                    if (c.Name != null && c.Name.Equals(info.CategoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedCat = c;
                        break;
                    }
                }

                if (matchedCat != null) return matchedCat.Id;

                _warnings.Add($"Category '{info.CategoryName}' not found.");
            }

            _warnings.Add($"No category specified. Defaulting to Walls.");
            return new ElementId((int)BuiltInCategory.OST_Walls);
        }

        private void SetParameterValue(Parameter param, object value)
        {
            if (value == null) return;

            switch (param.StorageType)
            {
                case StorageType.Integer:
                    if (value is long l) param.Set((int)l);
                    else if (value is int i) param.Set(i);
                    else if (value is bool b) param.Set(b ? 1 : 0);
                    break;
                case StorageType.Double:
                    if (value is double d) param.Set(d);
                    else if (value is long ld) param.Set((double)ld);
                    break;
                case StorageType.String:
                    param.Set(value.ToString());
                    break;
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Create Schedule";
    }
}
