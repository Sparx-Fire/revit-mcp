using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.DataExtraction;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views;

public class CreateFinishScheduleEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private FinishScheduleCreationInfo _info;
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public FinishScheduleCreationResult ResultInfo { get; private set; } = new FinishScheduleCreationResult();
    public bool TaskCompleted { get; private set; }

    public void SetParameters(FinishScheduleCreationInfo info)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
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
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();

        try
        {
            var exportHandler = new ExportRoomFinishDataEventHandler();
            exportHandler.SetParameters(
                _info.IncludeUnplacedRooms,
                _info.IncludeNotEnclosedRooms,
                includeMaterials: false);
            exportHandler.Execute(app);

            var exportResult = exportHandler.ResultInfo
                ?? throw new InvalidOperationException("Room finish export returned no result.");

            if (!exportResult.Success)
                throw new InvalidOperationException(exportResult.Message ?? "Room finish export failed.");

            var totalRooms = exportResult.TotalRooms;
            var missingRooms = exportResult.RoomsWithMissingFinishes;
            var missingRatio = totalRooms > 0 ? (double)missingRooms / totalRooms : 0;

            var threshold = _info.MissingFinishWarningThreshold > 0
                ? _info.MissingFinishWarningThreshold
                : 0.30;

            if (totalRooms > 0 && missingRatio > threshold)
            {
                warnings.Add(
                    $"{missingRooms} of {totalRooms} rooms ({missingRatio:P0}) lack finish data — exceeds {threshold:P0} threshold.");
            }

            if (exportResult.Warnings.Count > 0)
                warnings.AddRange(exportResult.Warnings);

            var scheduleInfo = BuildScheduleCreationInfo(app.ActiveUIDocument.Document, _info, warnings);
            var scheduleHandler = new CreateScheduleEventHandler();
            scheduleHandler.SetParameters(scheduleInfo);
            scheduleHandler.Execute(app);

            var scheduleResult = scheduleHandler.ResultInfo;
            if (scheduleResult == null || !scheduleResult.Success)
            {
                throw new InvalidOperationException(
                    scheduleResult?.Message ?? "Failed to create room finish schedule.");
            }

            if (scheduleResult.Warnings.Count > 0)
                warnings.AddRange(scheduleResult.Warnings);

            stopwatch.Stop();

            ResultInfo = new FinishScheduleCreationResult
            {
                Success = true,
                Message =
                    $"Successfully created finish schedule '{scheduleResult.ScheduleName}' for {totalRooms} rooms in {stopwatch.ElapsedMilliseconds} ms",
                ScheduleId = scheduleResult.ScheduleId,
                ScheduleUniqueId = scheduleResult.ScheduleUniqueId,
                ScheduleName = scheduleResult.ScheduleName,
                TemplateId = _info.TemplateId ?? string.Empty,
                TotalRooms = totalRooms,
                RoomsWithMissingFinishes = missingRooms,
                MissingFinishRatio = Math.Round(missingRatio, 4),
                FieldCount = scheduleResult.FieldCount,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ResultInfo = new FinishScheduleCreationResult
            {
                Success = false,
                Message = $"Error creating finish schedule: {ex.Message}",
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Warnings = warnings
            };
        }
        finally
        {
            TaskCompleted = true;
            _resetEvent.Set();
        }
    }

    private static ScheduleCreationInfo BuildScheduleCreationInfo(
        Document doc,
        FinishScheduleCreationInfo info,
        List<string> warnings)
    {
        var template = ResolveTemplateSchedule(doc, info.TemplateId);
        var hasTemplate = template != null;
        var scheduleInfo = new ScheduleCreationInfo
        {
            Name = string.IsNullOrWhiteSpace(info.Name) ? "Room Finish Schedule" : info.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(info.Type) ? "Regular" : info.Type.Trim(),
            CategoryName = "Rooms",
            TemplateId = info.TemplateId ?? string.Empty,
            ClearExistingFilters = false,
            ClearExistingSorts = !hasTemplate,
            ClearExistingGroups = !hasTemplate,
            ShowTitle = true,
            ShowHeaders = true,
            ShowGridLines = true,
            SortFields = new List<ScheduleSortInfo>
            {
                new ScheduleSortInfo { FieldName = "Number", SortOrder = "Ascending" }
            }
        };

        if (hasTemplate)
        {
            warnings.Add($"Using project schedule template '{template.Name}'.");
            scheduleInfo.Fields = new List<ScheduleFieldInfo>();
            scheduleInfo.SortFields = new List<ScheduleSortInfo>();
            return scheduleInfo;
        }

        if (!string.IsNullOrWhiteSpace(info.TemplateId))
            warnings.Add($"Template '{info.TemplateId}' was not found; created schedule with default finish columns.");

        scheduleInfo.Fields = new List<ScheduleFieldInfo>
        {
            new() { ParameterName = "Number", FieldType = "Instance", Heading = "№" },
            new() { ParameterName = "Name", FieldType = "Instance", Heading = "Помещение" },
            new() { ParameterName = "Level", FieldType = "Instance", Heading = "Уровень" },
            new() { ParameterName = "Area", FieldType = "Instance", Heading = "Площадь" },
            new() { ParameterName = "Floor Finish", FieldType = "Instance", Heading = "Пол" },
            new() { ParameterName = "Wall Finish", FieldType = "Instance", Heading = "Стены" },
            new() { ParameterName = "Ceiling Finish", FieldType = "Instance", Heading = "Потолок" }
        };

        return scheduleInfo;
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

    public string GetName() => "Create Finish Schedule";
}
