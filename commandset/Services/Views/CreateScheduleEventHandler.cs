using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views;

public class CreateScheduleEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private ScheduleCreationInfo _scheduleInfo;
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public ScheduleCreationResult ResultInfo { get; private set; } = new ScheduleCreationResult();
    public bool TaskCompleted { get; private set; }

    public void SetParameters(ScheduleCreationInfo scheduleInfo)
    {
        _scheduleInfo = scheduleInfo ?? throw new ArgumentNullException(nameof(scheduleInfo));
        TaskCompleted = false;
        _resetEvent.Reset();
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        var warnings = new List<string>();

        try
        {
            var doc = app.ActiveUIDocument.Document;
            ViewSchedule schedule;

            using (var tx = new Transaction(doc, "Create Schedule"))
            {
                tx.Start();
                schedule = CreateOrDuplicateSchedule(doc, _scheduleInfo, warnings);
                ConfigureScheduleDefinition(doc, schedule, _scheduleInfo, warnings);
                ApplyDisplayOptions(schedule, _scheduleInfo, warnings);
                SyncViewPhase(doc, schedule, app.ActiveUIDocument?.ActiveView, _scheduleInfo, warnings);
                tx.Commit();
            }

            RefreshScheduleView(app, schedule, warnings);

            var definition = schedule.Definition;
            ResultInfo = new ScheduleCreationResult
            {
                Success = true,
                Message = $"Successfully created schedule '{schedule.Name}'",
                ScheduleId = GetElementIdValue(schedule.Id),
                ScheduleUniqueId = schedule.UniqueId,
                ScheduleName = schedule.Name,
                CategoryName = _scheduleInfo.CategoryName,
                TemplateId = _scheduleInfo.TemplateId ?? string.Empty,
                FieldCount = definition.GetFieldCount(),
                FilterCount = definition.GetFilterCount(),
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            ResultInfo = new ScheduleCreationResult
            {
                Success = false,
                Message = $"Error creating schedule: {ex.Message}",
                Warnings = warnings
            };
        }
        finally
        {
            TaskCompleted = true;
            _resetEvent.Set();
        }
    }

    private static ViewSchedule CreateOrDuplicateSchedule(
        Document doc,
        ScheduleCreationInfo info,
        List<string> warnings)
    {
        var template = ResolveTemplateSchedule(doc, info.TemplateId);
        if (template != null)
        {
#if REVIT2024_OR_GREATER
            var duplicatedId = template.Duplicate(ViewDuplicateOption.Duplicate);
#else
            var duplicatedId = template.Duplicate(ViewDuplicateOption.Duplicate);
#endif
            var schedule = doc.GetElement(duplicatedId) as ViewSchedule
                ?? throw new InvalidOperationException("Failed to duplicate schedule template.");

            schedule.Name = GetUniqueScheduleName(doc, info.Name, template.Name);
            warnings.Add($"Duplicated schedule template '{template.Name}'.");
            return schedule;
        }

        if (!string.IsNullOrWhiteSpace(info.TemplateId))
            warnings.Add($"Template '{info.TemplateId}' was not found; created a new schedule instead.");

        var categoryId = ResolveCategoryId(info);
        var scheduleType = (info.Type ?? "Regular").Trim();

        ViewSchedule created = scheduleType.Equals("KeySchedule", StringComparison.OrdinalIgnoreCase)
            ? ViewSchedule.CreateKeySchedule(doc, categoryId)
            : ViewSchedule.CreateSchedule(doc, categoryId);

        created.Name = GetUniqueScheduleName(doc, info.Name, info.CategoryName);
        return created;
    }

    private static void ConfigureScheduleDefinition(
        Document doc,
        ViewSchedule schedule,
        ScheduleCreationInfo info,
        List<string> warnings)
    {
        var definition = schedule.Definition;

        if (info.Fields != null && info.Fields.Count > 0)
            ApplyFields(doc, definition, info.Fields, warnings);

        if (info.ClearExistingFilters)
            ClearFilters(definition);

        if (info.Filters != null && info.Filters.Count > 0)
            ApplyFilters(doc, definition, info.Filters, warnings);

        if (info.ClearExistingSorts || info.ClearExistingGroups)
            ClearSortGroupFields(definition);

        if (info.SortFields != null && info.SortFields.Count > 0)
            ApplySortFields(doc, definition, info.SortFields, warnings);

        if (info.GroupFields != null && info.GroupFields.Count > 0)
            ApplyGroupFields(doc, definition, info.GroupFields, warnings);
    }

    private static void ApplyFields(
        Document doc,
        ScheduleDefinition definition,
        List<ScheduleFieldInfo> fields,
        List<string> warnings)
    {
        foreach (var fieldInfo in fields)
        {
            try
            {
                if (fieldInfo.IsHidden)
                    continue;

                var schedulableField = FindSchedulableField(doc, definition, fieldInfo);
                if (schedulableField == null)
                {
                    warnings.Add($"Field '{fieldInfo.ParameterName}' was not found and was skipped.");
                    continue;
                }

                var field = definition.AddField(schedulableField);
                if (!string.IsNullOrWhiteSpace(fieldInfo.Heading))
                    field.ColumnHeading = fieldInfo.Heading;

                if (fieldInfo.Width > 0)
                    field.GridColumnWidth = fieldInfo.Width / 304.8;

                ApplyHorizontalAlignment(field, fieldInfo.HorizontalAlignment);
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to add field '{fieldInfo.ParameterName}': {ex.Message}");
            }
        }
    }

    private static void ApplyFilters(
        Document doc,
        ScheduleDefinition definition,
        List<ScheduleFilterInfo> filters,
        List<string> warnings)
    {
        foreach (var filterInfo in filters)
        {
            try
            {
                var fieldId = ResolveScheduleFieldId(doc, definition, filterInfo.FieldName, filterInfo.FieldIndex);
                if (fieldId == null)
                {
                    warnings.Add($"Filter field '{filterInfo.FieldName}' was not found and was skipped.");
                    continue;
                }

                var filterType = ParseFilterType(filterInfo.FilterType);
                definition.AddFilter(new ScheduleFilter(fieldId, filterType, filterInfo.FilterValue ?? string.Empty));
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to add filter '{filterInfo.FieldName}': {ex.Message}");
            }
        }
    }

    private static void ApplySortFields(
        Document doc,
        ScheduleDefinition definition,
        List<ScheduleSortInfo> sortFields,
        List<string> warnings)
    {
        foreach (var sortInfo in sortFields)
        {
            try
            {
                var fieldId = ResolveScheduleFieldId(doc, definition, sortInfo.FieldName, sortInfo.FieldIndex);
                if (fieldId == null)
                {
                    warnings.Add($"Sort field '{sortInfo.FieldName}' was not found and was skipped.");
                    continue;
                }

                var sortGroupField = new ScheduleSortGroupField(fieldId, ParseSortOrder(sortInfo.SortOrder));
                definition.AddSortGroupField(sortGroupField);
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to add sort field '{sortInfo.FieldName}': {ex.Message}");
            }
        }
    }

    private static void ApplyGroupFields(
        Document doc,
        ScheduleDefinition definition,
        List<ScheduleGroupInfo> groupFields,
        List<string> warnings)
    {
        foreach (var groupInfo in groupFields)
        {
            try
            {
                var fieldId = ResolveScheduleFieldId(doc, definition, groupInfo.FieldName, groupInfo.FieldIndex);
                if (fieldId == null)
                {
                    warnings.Add($"Group field '{groupInfo.FieldName}' was not found and was skipped.");
                    continue;
                }

                var sortGroupField = new ScheduleSortGroupField(fieldId, ParseSortOrder(groupInfo.SortOrder))
                {
                    ShowHeader = groupInfo.ShowHeader,
                    ShowFooter = groupInfo.ShowFooter,
                    ShowBlankLine = groupInfo.ShowBlankLine
                };
                definition.AddSortGroupField(sortGroupField);
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to add group field '{groupInfo.FieldName}': {ex.Message}");
            }
        }
    }

    private static void SyncViewPhase(
        Document doc,
        ViewSchedule schedule,
        View activeView,
        ScheduleCreationInfo info,
        List<string> warnings)
    {
        try
        {
            var targetPhaseId = activeView?.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsElementId();
            if (targetPhaseId == null || targetPhaseId == ElementId.InvalidElementId)
                targetPhaseId = null;

            if (targetPhaseId != null)
                SetViewPhase(schedule, targetPhaseId);

            if (IsRoomsCategory(info) && CountRoomsInSchedule(doc, schedule) == 0)
            {
                var roomPhaseId = GetPrimaryRoomPhaseId(doc);
                if (roomPhaseId != null && roomPhaseId != ElementId.InvalidElementId)
                {
                    SetViewPhase(schedule, roomPhaseId);
                    var phaseName = doc.GetElement(roomPhaseId)?.Name ?? roomPhaseId.ToString();
                    warnings.Add(
                        $"Schedule view phase set to '{phaseName}' so placed rooms appear in the schedule.");
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to sync schedule view phase: {ex.Message}");
        }
    }

    private static void SetViewPhase(View view, ElementId phaseId)
    {
        var phaseParam = view.get_Parameter(BuiltInParameter.VIEW_PHASE);
        if (phaseParam != null && !phaseParam.IsReadOnly)
            phaseParam.Set(phaseId);
    }

    private static int CountRoomsInSchedule(Document doc, ViewSchedule schedule)
    {
        return new FilteredElementCollector(doc, schedule.Id)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .GetElementCount();
    }

    private static ElementId GetPrimaryRoomPhaseId(Document doc)
    {
        var room = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .FirstOrDefault(element => element.get_Parameter(BuiltInParameter.ROOM_AREA)?.AsDouble() > 0);

        return room?.get_Parameter(BuiltInParameter.ROOM_PHASE)?.AsElementId();
    }

    private static bool IsRoomsCategory(ScheduleCreationInfo info)
    {
        if (info.CategoryId > 0)
            return info.CategoryId == (int)BuiltInCategory.OST_Rooms;

        return info.CategoryName?.Trim().Equals("Rooms", StringComparison.OrdinalIgnoreCase) == true
            || info.CategoryName?.Trim().Equals("Room", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void RefreshScheduleView(UIApplication app, ViewSchedule schedule, List<string> warnings)
    {
        var uiDoc = app.ActiveUIDocument;
        if (uiDoc == null)
            return;

        var previousView = uiDoc.ActiveView;
        try
        {
            uiDoc.ActiveView = schedule;
            uiDoc.RefreshActiveView();
        }
        catch (Exception ex)
        {
            warnings.Add($"Schedule created but view refresh skipped: {ex.Message}");
            return;
        }

        if (previousView != null && previousView.Id != schedule.Id)
        {
            try
            {
                uiDoc.ActiveView = previousView;
            }
            catch (Exception ex)
            {
                warnings.Add($"Restored previous view after schedule refresh: {ex.Message}");
            }
        }
    }

    private static void ApplyDisplayOptions(
        ViewSchedule schedule,
        ScheduleCreationInfo info,
        List<string> warnings)
    {
        try
        {
            var definition = schedule.Definition;
            if (info.ShowTitle.HasValue)
                definition.ShowTitle = info.ShowTitle.Value;
            if (info.ShowHeaders.HasValue)
                definition.ShowHeaders = info.ShowHeaders.Value;
            if (info.ShowGridLines.HasValue)
                definition.ShowGridLines = info.ShowGridLines.Value;
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to apply display options: {ex.Message}");
        }
    }

    private static ViewSchedule ResolveTemplateSchedule(Document doc, string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            return null;

        Element element = doc.GetElement(templateId);
        if (element == null && int.TryParse(templateId, out var numericId))
            element = doc.GetElement(new ElementId(numericId));

        return element as ViewSchedule;
    }

    private static ElementId ResolveCategoryId(ScheduleCreationInfo info)
    {
        if (info.CategoryId > 0)
            return new ElementId(info.CategoryId);

        if (!string.IsNullOrWhiteSpace(info.CategoryName) &&
            Enum.TryParse(info.CategoryName, true, out BuiltInCategory builtInCategory))
            return new ElementId(builtInCategory);

        return info.CategoryName?.Trim().ToLowerInvariant() switch
        {
            "door" or "doors" => new ElementId(BuiltInCategory.OST_Doors),
            "window" or "windows" => new ElementId(BuiltInCategory.OST_Windows),
            "room" or "rooms" => new ElementId(BuiltInCategory.OST_Rooms),
            "floor" or "floors" => new ElementId(BuiltInCategory.OST_Floors),
            _ => throw new ArgumentException(
                $"Unable to resolve schedule category from '{info.CategoryName}'. Provide categoryName or categoryId.")
        };
    }

    private static SchedulableField FindSchedulableField(
        Document doc,
        ScheduleDefinition definition,
        ScheduleFieldInfo fieldInfo)
    {
        var schedulableFields = definition.GetSchedulableFields();
        foreach (var schedulableField in schedulableFields)
        {
            if (fieldInfo.ParameterId > 0 &&
                GetElementIdValue(schedulableField.ParameterId) == fieldInfo.ParameterId)
                return schedulableField;

            var fieldType = ParseFieldType(fieldInfo.FieldType);
            if (schedulableField.FieldType != fieldType)
                continue;

            var name = schedulableField.GetName(doc);
            if (!string.IsNullOrWhiteSpace(fieldInfo.ParameterName) &&
                name.Equals(fieldInfo.ParameterName, StringComparison.OrdinalIgnoreCase))
                return schedulableField;
        }

        return null;
    }

    private static ScheduleFieldId ResolveScheduleFieldId(
        Document doc,
        ScheduleDefinition definition,
        string fieldName,
        int fieldIndex)
    {
        if (fieldIndex >= 0 && fieldIndex < definition.GetFieldCount())
            return definition.GetFieldId(fieldIndex);

        for (var i = 0; i < definition.GetFieldCount(); i++)
        {
            var field = definition.GetField(definition.GetFieldId(i));
            if (field.GetName().Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                return definition.GetFieldId(i);
        }

        var schedulableField = FindSchedulableField(
            doc,
            definition,
            new ScheduleFieldInfo { ParameterName = fieldName, FieldType = "Instance" });
        if (schedulableField == null)
            return null;

        var addedField = definition.AddField(schedulableField);
        return addedField.FieldId;
    }

    private static void ClearFilters(ScheduleDefinition definition)
    {
        definition.ClearFilters();
    }

    private static void ClearSortGroupFields(ScheduleDefinition definition)
    {
        definition.ClearSortGroupFields();
    }

    private static ScheduleFieldType ParseFieldType(string fieldType)
    {
        return fieldType?.Trim().ToLowerInvariant() switch
        {
            "type" or "elementtype" => ScheduleFieldType.ElementType,
            "count" => ScheduleFieldType.Count,
            "formula" => ScheduleFieldType.Formula,
            _ => ScheduleFieldType.Instance
        };
    }

    private static ScheduleFilterType ParseFilterType(string filterType)
    {
        return filterType?.Trim().ToLowerInvariant() switch
        {
            "notequal" or "not_equals" or "notequals" => ScheduleFilterType.NotEqual,
            "greaterthan" or "greater_than" => ScheduleFilterType.GreaterThan,
            "greaterthanorequal" or "greater_than_or_equal" => ScheduleFilterType.GreaterThanOrEqual,
            "lessthan" or "less_than" => ScheduleFilterType.LessThan,
            "lessthanorequal" or "less_than_or_equal" => ScheduleFilterType.LessThanOrEqual,
            "contains" => ScheduleFilterType.Contains,
            "notcontains" or "not_contains" => ScheduleFilterType.NotContains,
            "beginswith" or "begins_with" => ScheduleFilterType.BeginsWith,
            "endswith" or "ends_with" => ScheduleFilterType.EndsWith,
            _ => ScheduleFilterType.Equal
        };
    }

    private static ScheduleSortOrder ParseSortOrder(string sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() == "descending"
            ? ScheduleSortOrder.Descending
            : ScheduleSortOrder.Ascending;
    }

    private static void ApplyHorizontalAlignment(ScheduleField field, string alignment)
    {
        switch (alignment?.Trim().ToLowerInvariant())
        {
            case "center":
                field.HorizontalAlignment = ScheduleHorizontalAlignment.Center;
                break;
            case "right":
                field.HorizontalAlignment = ScheduleHorizontalAlignment.Right;
                break;
            default:
                field.HorizontalAlignment = ScheduleHorizontalAlignment.Left;
                break;
        }
    }

    private static string GetUniqueScheduleName(Document doc, string requestedName, string fallbackName)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName)
            ? $"{fallbackName} Schedule"
            : requestedName.Trim();

        var name = baseName;
        var suffix = 1;
        while (ScheduleNameExists(doc, name))
            name = $"{baseName} ({suffix++})";

        return name;
    }

    private static bool ScheduleNameExists(Document doc, string name)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(ViewSchedule))
            .Cast<ViewSchedule>()
            .Any(schedule => schedule.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static long GetElementIdValue(ElementId elementId)
    {
#if REVIT2024_OR_GREATER
        return elementId.Value;
#else
        return elementId.IntegerValue;
#endif
    }

    public string GetName() => "Create Schedule";
}
