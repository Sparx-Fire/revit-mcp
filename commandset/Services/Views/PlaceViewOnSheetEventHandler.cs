using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views;

public class PlaceViewOnSheetEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private ViewportCreationInfo _placementInfo;
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public ViewportPlacementResult ResultInfo { get; private set; } = new ViewportPlacementResult();
    public bool TaskCompleted { get; private set; }

    public void SetParameters(ViewportCreationInfo placementInfo)
    {
        _placementInfo = placementInfo ?? throw new ArgumentNullException(nameof(placementInfo));
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
            var sheet = ResolveViewSheet(doc, _placementInfo);
            var view = ResolveView(doc, _placementInfo);

            Element placedElement;
            string placementType;

            using (var tx = new Transaction(doc, "Place View On Sheet"))
            {
                tx.Start();

                if (view is ViewSchedule scheduleView)
                {
                    placedElement = PlaceSchedule(doc, sheet, scheduleView, _placementInfo, warnings);
                    placementType = "schedule";
                }
                else
                {
                    placedElement = PlaceViewport(doc, sheet, view, _placementInfo, warnings);
                    placementType = "viewport";
                }

                tx.Commit();
            }

            ResultInfo = new ViewportPlacementResult
            {
                Success = true,
                Message = $"Successfully placed '{view.Name}' on sheet '{sheet.SheetNumber}'",
                PlacementType = placementType,
                ElementId = GetElementIdValue(placedElement.Id),
                ElementUniqueId = placedElement.UniqueId,
                SheetId = GetElementIdValue(sheet.Id),
                ViewId = GetElementIdValue(view.Id),
                PositionX = _placementInfo.PositionX,
                PositionY = _placementInfo.PositionY,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            ResultInfo = new ViewportPlacementResult
            {
                Success = false,
                Message = $"Error placing view on sheet: {ex.Message}",
                PositionX = _placementInfo?.PositionX ?? 0,
                PositionY = _placementInfo?.PositionY ?? 0,
                Warnings = warnings
            };
        }
        finally
        {
            TaskCompleted = true;
            _resetEvent.Set();
        }
    }

    private static Element PlaceSchedule(
        Document doc,
        ViewSheet sheet,
        ViewSchedule scheduleView,
        ViewportCreationInfo info,
        List<string> warnings)
    {
        var alreadyPlaced = new FilteredElementCollector(doc, sheet.Id)
            .OfClass(typeof(ScheduleSheetInstance))
            .Cast<ScheduleSheetInstance>()
            .Any(instance => instance.ScheduleId == scheduleView.Id);

        if (alreadyPlaced)
            warnings.Add($"Schedule '{scheduleView.Name}' is already placed on this sheet.");

        var sheetOutline = sheet.Outline
            ?? throw new InvalidOperationException($"Sheet '{sheet.SheetNumber}' has no outline.");
        var requestedLowerLeft = GetRequestedLowerLeftPoint(sheetOutline, info);
        var origin = new XYZ(requestedLowerLeft.X, requestedLowerLeft.Y, 0);
        var instance = ScheduleSheetInstance.Create(doc, sheet.Id, scheduleView.Id, origin);
        doc.Regenerate();
        ClampScheduleToSheet(doc, sheet, instance, info, warnings);

        if (Math.Abs(info.Rotation) > double.Epsilon)
            warnings.Add("Schedule rotation is not supported via API and was ignored.");

        return instance;
    }

    private static Viewport PlaceViewport(
        Document doc,
        ViewSheet sheet,
        View view,
        ViewportCreationInfo info,
        List<string> warnings)
    {
        var existingViewport = new FilteredElementCollector(doc, sheet.Id)
            .OfClass(typeof(Viewport))
            .Cast<Viewport>()
            .Any(viewport => viewport.ViewId == view.Id);

        if (existingViewport)
            warnings.Add($"View '{view.Name}' is already placed on this sheet.");

        Viewport viewport;
        try
        {
            viewport = Viewport.Create(doc, sheet.Id, view.Id, GetSheetCenter(sheet));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"View '{view.Name}' cannot be placed on sheet '{sheet.SheetNumber}': {ex.Message}",
                ex);
        }

        if (info.ViewportTypeId > 0)
        {
            try
            {
                viewport.ChangeTypeId(new ElementId(info.ViewportTypeId));
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to apply viewport type: {ex.Message}");
            }
        }

        if (info.DisplayTitle.HasValue)
            ApplyViewportDisplayTitle(viewport, info.DisplayTitle.Value, warnings);

        if (info.ScaleOverride > 0)
            ApplyViewportScale(view, info.ScaleOverride, warnings);

        if (!string.IsNullOrWhiteSpace(info.LabelText))
            warnings.Add("Custom viewport label text is not supported and was ignored.");

        if (Math.Abs(info.Rotation) > double.Epsilon)
            warnings.Add("Viewport rotation is not supported in this command and was ignored.");

        doc.Regenerate();
        MoveViewportToRequestedLocation(viewport, sheet, info, warnings);

        return viewport;
    }

    private static XYZ GetSheetCenter(ViewSheet sheet)
    {
        var outline = sheet.Outline
            ?? throw new InvalidOperationException($"Sheet '{sheet.SheetNumber}' has no outline.");

        var centerX = (outline.Min.U + outline.Max.U) / 2.0;
        var centerY = (outline.Min.V + outline.Max.V) / 2.0;
        return new XYZ(centerX, centerY, 0);
    }

    private static void MoveViewportToRequestedLocation(
        Viewport viewport,
        ViewSheet sheet,
        ViewportCreationInfo info,
        List<string> warnings)
    {
        var sheetOutline = sheet.Outline
            ?? throw new InvalidOperationException($"Sheet '{sheet.SheetNumber}' has no outline.");

        var requestedLowerLeft = GetRequestedLowerLeftPoint(sheetOutline, info);
        var requestedCenter = GetCenterForLowerLeft(requestedLowerLeft, viewport.GetBoxOutline());
        viewport.SetBoxCenter(requestedCenter);

        var movedOutline = viewport.GetBoxOutline();
        var clampedLowerLeft = ClampLowerLeftToSheetBounds(movedOutline, sheetOutline, requestedLowerLeft);

        if (HasPositionChanged(requestedLowerLeft, clampedLowerLeft))
        {
            warnings.Add(BuildClampWarning(
                "Viewport",
                requestedLowerLeft,
                clampedLowerLeft,
                sheetOutline));
        }

        viewport.SetBoxCenter(GetCenterForLowerLeft(clampedLowerLeft, movedOutline));
    }

    private static void ClampScheduleToSheet(
        Document doc,
        ViewSheet sheet,
        ScheduleSheetInstance instance,
        ViewportCreationInfo info,
        List<string> warnings)
    {
        var sheetOutline = sheet.Outline
            ?? throw new InvalidOperationException($"Sheet '{sheet.SheetNumber}' has no outline.");
        var requestedLowerLeft = GetRequestedLowerLeftPoint(sheetOutline, info);
        var scheduleOutline = GetElementOutlineOnSheet(instance, sheet);
        if (scheduleOutline == null)
            return;

        var clampedLowerLeft = ClampLowerLeftToSheetBounds(scheduleOutline, sheetOutline, requestedLowerLeft);
        if (!HasPositionChanged(requestedLowerLeft, clampedLowerLeft))
            return;

        ElementTransformUtils.MoveElement(
            doc,
            instance.Id,
            new XYZ(
                clampedLowerLeft.X - requestedLowerLeft.X,
                clampedLowerLeft.Y - requestedLowerLeft.Y,
                0));

        warnings.Add(BuildClampWarning(
            "Schedule",
            requestedLowerLeft,
            clampedLowerLeft,
            sheetOutline));
    }

    private static Outline GetElementOutlineOnSheet(Element element, ViewSheet sheet)
    {
        var bbox = element.get_BoundingBox(sheet);
        if (bbox == null)
            return null;

        return new Outline(
            new XYZ(bbox.Min.X, bbox.Min.Y, 0),
            new XYZ(bbox.Max.X, bbox.Max.Y, 0));
    }

    private static XYZ GetRequestedLowerLeftPoint(BoundingBoxUV sheetOutline, ViewportCreationInfo info)
    {
        return new XYZ(
            sheetOutline.Min.U + MmToFeet(info.PositionX),
            sheetOutline.Min.V + MmToFeet(info.PositionY),
            0);
    }

    private static XYZ GetCenterForLowerLeft(XYZ lowerLeft, Outline elementOutline)
    {
        var width = elementOutline.MaximumPoint.X - elementOutline.MinimumPoint.X;
        var height = elementOutline.MaximumPoint.Y - elementOutline.MinimumPoint.Y;
        return new XYZ(lowerLeft.X + width / 2.0, lowerLeft.Y + height / 2.0, 0);
    }

    private static XYZ ClampLowerLeftToSheetBounds(Outline elementOutline, BoundingBoxUV sheetOutline, XYZ requestedLowerLeft)
    {
        var width = elementOutline.MaximumPoint.X - elementOutline.MinimumPoint.X;
        var height = elementOutline.MaximumPoint.Y - elementOutline.MinimumPoint.Y;

        var minX = sheetOutline.Min.U;
        var maxX = sheetOutline.Max.U - width;
        var minY = sheetOutline.Min.V;
        var maxY = sheetOutline.Max.V - height;

        var targetX = ClampOrCenter(requestedLowerLeft.X, minX, maxX, sheetOutline.Min.U, sheetOutline.Max.U - width);
        var targetY = ClampOrCenter(requestedLowerLeft.Y, minY, maxY, sheetOutline.Min.V, sheetOutline.Max.V - height);
        return new XYZ(targetX, targetY, 0);
    }

    private static bool HasPositionChanged(XYZ requested, XYZ actual)
    {
        return Math.Abs(requested.X - actual.X) > 1e-9 || Math.Abs(requested.Y - actual.Y) > 1e-9;
    }

    private static string BuildClampWarning(
        string elementType,
        XYZ requestedLowerLeft,
        XYZ finalLowerLeft,
        BoundingBoxUV sheetOutline)
    {
        var requestedX = FeetToMm(requestedLowerLeft.X - sheetOutline.Min.U);
        var requestedY = FeetToMm(requestedLowerLeft.Y - sheetOutline.Min.V);
        var finalX = FeetToMm(finalLowerLeft.X - sheetOutline.Min.U);
        var finalY = FeetToMm(finalLowerLeft.Y - sheetOutline.Min.V);
        return $"{elementType} position was clamped to sheet bounds: requested=({requestedX:F1}, {requestedY:F1}) mm, actual=({finalX:F1}, {finalY:F1}) mm.";
    }

    private static double ClampOrCenter(
        double value,
        double minValue,
        double maxValue,
        double fallbackMin,
        double fallbackMax)
    {
        if (minValue > maxValue)
            return (fallbackMin + fallbackMax) / 2.0;

        return Clamp(value, minValue, maxValue);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
    }

    private static void ApplyViewportDisplayTitle(Viewport viewport, bool displayTitle, List<string> warnings)
    {
        try
        {
            var param = viewport.get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL);
            if (param != null && !param.IsReadOnly)
                param.Set(displayTitle ? 1 : 0);
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to set viewport title visibility: {ex.Message}");
        }
    }

    private static void ApplyViewportScale(View view, int scaleOverride, List<string> warnings)
    {
        try
        {
            if (view.Scale != scaleOverride)
                view.Scale = scaleOverride;
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to override view scale: {ex.Message}");
        }
    }

    private static ViewSheet ResolveViewSheet(Document doc, ViewportCreationInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.SheetUniqueId))
        {
            var sheet = doc.GetElement(info.SheetUniqueId) as ViewSheet;
            if (sheet != null)
                return sheet;
        }

        if (info.SheetId > 0)
        {
            var sheet = doc.GetElement(new ElementId(info.SheetId)) as ViewSheet;
            if (sheet != null)
                return sheet;
        }

        throw new ArgumentException("A valid sheetId or sheetUniqueId is required.");
    }

    private static View ResolveView(Document doc, ViewportCreationInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.ViewUniqueId))
        {
            var view = doc.GetElement(info.ViewUniqueId) as View;
            if (view != null)
                return view;
        }

        if (info.ViewId > 0)
        {
            var view = doc.GetElement(new ElementId(info.ViewId)) as View;
            if (view != null)
                return view;
        }

        throw new ArgumentException("A valid viewId or viewUniqueId is required.");
    }

    private static double MmToFeet(double millimeters) => millimeters / 304.8;
    private static double FeetToMm(double feet) => feet * 304.8;

    private static long GetElementIdValue(ElementId elementId)
    {
#if REVIT2024_OR_GREATER
        return elementId.Value;
#else
        return elementId.IntegerValue;
#endif
    }

    public string GetName() => "Place View On Sheet";
}
