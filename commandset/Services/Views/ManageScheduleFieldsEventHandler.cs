using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class ManageScheduleFieldsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ScheduleId { get; private set; }
        public string Action { get; private set; }
        public string FieldName { get; private set; }
        public int? Position { get; private set; }

        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int scheduleId, string action, string fieldName, int? position)
        {
            ScheduleId = scheduleId;
            Action = action;
            FieldName = fieldName;
            Position = position;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Manage Schedule Fields"))
                {
                    trans.Start();

                    ViewSchedule schedule = doc.GetElement(new ElementId(ScheduleId)) as ViewSchedule;
                    if (schedule == null)
                    {
                        Result = new AIResult<bool> { Success = false, Message = $"Schedule with ID {ScheduleId} not found" };
                        return;
                    }

                    ScheduleDefinition definition = schedule.Definition;
                    IList<ScheduleFieldId> fieldOrder = definition.GetFieldOrder();

                    switch (Action.ToLowerInvariant())
                    {
                        case "add":
                        {
                            ScheduleFieldType fieldType = ScheduleFieldType.Instance;

                            // Try to find field by name in schedulable fields first
                            var schedulableFields = definition.GetSchedulableFields();
                            SchedulableField targetSf = null;
                            foreach (var sf in schedulableFields)
                            {
                                if (sf.GetName(doc) == FieldName)
                                {
                                    targetSf = sf;
                                    break;
                                }
                            }

                            if (targetSf != null)
                            {
                                ScheduleField field = definition.AddField(targetSf);
                                if (field != null && Position.HasValue)
                                {
                                    ScheduleFieldId fieldId = field.FieldId;
                                    fieldOrder.Insert(Position.Value, fieldId);
                                    definition.SetFieldOrder(fieldOrder);
                                }
                            }
                            else
                            {
                                Result = new AIResult<bool> { Success = false, Message = $"Field '{FieldName}' not found" };
                                return;
                            }
                            break;
                        }
                        case "remove":
                        {
                            foreach (var fieldId in fieldOrder)
                            {
                                ScheduleField field = definition.GetField(fieldId);
                                if (field.GetSchedulableField().GetName(doc) == FieldName)
                                {
                                    definition.RemoveField(fieldId);
                                    break;
                                }
                            }
                            break;
                        }
                        case "reorder":
                        {
                            if (Position.HasValue)
                            {
                                ScheduleFieldId targetFieldId = null;
                                foreach (var fieldId in fieldOrder)
                                {
                                    ScheduleField field = definition.GetField(fieldId);
                                    if (field.GetSchedulableField().GetName(doc) == FieldName)
                                    {
                                        targetFieldId = fieldId;
                                        break;
                                    }
                                }

                                if (targetFieldId != null)
                                {
                                    fieldOrder.Remove(targetFieldId);
                                    int insertPos = Math.Min(Position.Value, fieldOrder.Count);
                                    fieldOrder.Insert(insertPos, targetFieldId);
                                    definition.SetFieldOrder(fieldOrder);
                                }
                            }
                            break;
                        }
                        case "hide":
                        {
                            foreach (var fieldId in fieldOrder)
                            {
                                ScheduleField field = definition.GetField(fieldId);
                                if (field.GetSchedulableField().GetName(doc) == FieldName)
                                {
                                    VersionCompat.SetScheduleFieldVisibility(definition, fieldId, false);
                                    break;
                                }
                            }
                            break;
                        }
                        case "show":
                        {
                            foreach (var fieldId in fieldOrder)
                            {
                                ScheduleField field = definition.GetField(fieldId);
                                if (field.GetSchedulableField().GetName(doc) == FieldName)
                                {
                                    VersionCompat.SetScheduleFieldVisibility(definition, fieldId, true);
                                    break;
                                }
                            }
                            break;
                        }
                    }

                    trans.Commit();

                    Result = new AIResult<bool>
                    {
                        Success = true,
                        Message = $"Schedule field '{FieldName}' {Action}ed successfully",
                        Response = true
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool>
                {
                    Success = false,
                    Message = $"Error managing schedule fields: {ex.Message}",
                    Response = false
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private Parameter FindScheduleParameter(ScheduleDefinition definition, string fieldName)
        {
            ElementId categoryId = VersionCompat.GetScheduleCategoryId(definition);
            if (categoryId != ElementId.InvalidElementId)
            {
                Category category = Category.GetCategory(doc, categoryId);

                if (category != null)
                {
                    foreach (Parameter param in VersionCompat.GetCategoryParameters(category))
                    {
                        if (param.Definition.Name == fieldName)
                        {
                            return param;
                        }
                    }
                }
            }

            foreach (Parameter param in doc.ProjectInformation.Parameters)
            {
                if (param.Definition.Name == fieldName)
                {
                    return param;
                }
            }

            return null;
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Manage Schedule Fields";
    }
}
