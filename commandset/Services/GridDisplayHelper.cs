using Autodesk.Revit.DB;
using RevitMCPCommandSet.Models.Architecture;

namespace RevitMCPCommandSet.Services;

public static class GridDisplayHelper
{
    private const double MillimetersToFeet = 1.0 / 304.8;

    private sealed class ExtentBounds
    {
        public double MinXFeet { get; set; }
        public double MaxXFeet { get; set; }
        public double MinYFeet { get; set; }
        public double MaxYFeet { get; set; }
    }

    public static GridDisplayConfigurationResult ConfigureGrids(
        Document doc,
        IEnumerable<Grid> grids,
        GridDisplayConfigurationInfo options)
    {
        var warnings = new List<string>();
        var gridList = grids.Where(grid => grid != null).ToList();
        if (gridList.Count == 0)
            throw new InvalidOperationException("No grids were found to configure.");

        var gridType = ResolveGridType(doc, options.GridTypeName, options.GridTypeId);
        if (gridType == null)
            warnings.Add("No GridType was resolved; bubble style was not changed.");

        var views = options.ApplyToAllFloorPlans
            ? GetFloorPlans(doc)
            : new List<ViewPlan>();

        if (views.Count == 0)
            warnings.Add("No floor plan views were found.");

        var bounds = ComputeExtentBounds(gridList, options);
        var gridViewUpdates = 0;

        foreach (var grid in gridList)
        {
            if (gridType != null && grid.GetTypeId() != gridType.Id)
                grid.ChangeTypeId(gridType.Id);

            foreach (var view in views)
            {
                if (!grid.CanBeVisibleInView(view))
                    continue;

                try
                {
                    ApplyGridExtentInView(grid, view, bounds);

                    if (options.ShowBubbles)
                    {
                        grid.ShowBubbleInView(DatumEnds.End0, view);
                        grid.ShowBubbleInView(DatumEnds.End1, view);
                    }
                    else
                    {
                        grid.HideBubbleInView(DatumEnds.End0, view);
                        grid.HideBubbleInView(DatumEnds.End1, view);
                    }

                    gridViewUpdates++;
                }
                catch (Exception ex)
                {
                    warnings.Add($"Grid '{grid.Name}' on view '{view.Name}': {ex.Message}");
                }
            }
        }

        return new GridDisplayConfigurationResult
        {
            GridsProcessed = gridList.Count,
            ViewsProcessed = views.Count,
            GridViewUpdates = gridViewUpdates,
            GridTypeName = gridType?.Name ?? string.Empty,
            Warnings = warnings
        };
    }

    public static GridDisplayConfigurationInfo FromCreationInfo(GridCreationInfo creationInfo)
    {
        return new GridDisplayConfigurationInfo
        {
            GridTypeName = creationInfo.GridTypeName ?? string.Empty,
            GridTypeId = creationInfo.GridTypeId,
            XExtentMin = creationInfo.XExtentMin,
            XExtentMax = creationInfo.XExtentMax,
            YExtentMin = creationInfo.YExtentMin,
            YExtentMax = creationInfo.YExtentMax,
            ShowBubbles = creationInfo.ShowBubbles,
            ApplyToAllFloorPlans = creationInfo.ConfigureDisplayOnAllPlans
        };
    }

    public static IList<Grid> GetGrids(Document doc, IEnumerable<long> gridIds)
    {
        if (gridIds != null && gridIds.Any())
        {
            return gridIds
                .Select(id =>
                {
#if REVIT2024_OR_GREATER
                    return doc.GetElement(new ElementId(id)) as Grid;
#else
                    return doc.GetElement(new ElementId((int)id)) as Grid;
#endif
                })
                .Where(grid => grid != null)
                .ToList();
        }

        return new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToList();
    }

    public static GridType ResolveGridType(Document doc, string gridTypeName, int gridTypeId)
    {
        if (gridTypeId > 0)
        {
            var byId = doc.GetElement(new ElementId(gridTypeId)) as GridType;
            if (byId != null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(gridTypeName))
        {
            var trimmed = gridTypeName.Trim();
            var types = new FilteredElementCollector(doc)
                .OfClass(typeof(GridType))
                .Cast<GridType>()
                .ToList();

            var exact = types.FirstOrDefault(
                type => type.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
                return exact;

            var partial = types.FirstOrDefault(
                type => type.Name.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0);
            if (partial != null)
                return partial;
        }

        return new FilteredElementCollector(doc)
            .OfClass(typeof(GridType))
            .Cast<GridType>()
            .FirstOrDefault();
    }

    public static IList<ViewPlan> GetFloorPlans(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .Where(view => !view.IsTemplate && view.ViewType == ViewType.FloorPlan)
            .ToList();
    }

    private static ExtentBounds ComputeExtentBounds(
        IReadOnlyCollection<Grid> grids,
        GridDisplayConfigurationInfo options)
    {
        if (options.XExtentMin.HasValue &&
            options.XExtentMax.HasValue &&
            options.YExtentMin.HasValue &&
            options.YExtentMax.HasValue &&
            options.XExtentMin.Value < options.XExtentMax.Value &&
            options.YExtentMin.Value < options.YExtentMax.Value)
        {
            return new ExtentBounds
            {
                MinXFeet = options.XExtentMin.Value * MillimetersToFeet,
                MaxXFeet = options.XExtentMax.Value * MillimetersToFeet,
                MinYFeet = options.YExtentMin.Value * MillimetersToFeet,
                MaxYFeet = options.YExtentMax.Value * MillimetersToFeet
            };
        }

        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var minY = double.MaxValue;
        var maxY = double.MinValue;

        foreach (var grid in grids)
        {
            if (grid.Curve is not Line line)
                continue;

            foreach (var point in new[] { line.GetEndPoint(0), line.GetEndPoint(1) })
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }
        }

        if (minX > maxX || minY > maxY)
            throw new InvalidOperationException("Unable to compute grid extents from model curves.");

        const double paddingFeet = 1.0;
        return new ExtentBounds
        {
            MinXFeet = minX - paddingFeet,
            MaxXFeet = maxX + paddingFeet,
            MinYFeet = minY - paddingFeet,
            MaxYFeet = maxY + paddingFeet
        };
    }

    private static void ApplyGridExtentInView(Grid grid, ViewPlan view, ExtentBounds bounds)
    {
        if (grid.Curve is not Line modelLine)
            return;

        var direction = (modelLine.GetEndPoint(1) - modelLine.GetEndPoint(0)).Normalize();
        Line viewCurve;

        if (Math.Abs(direction.X) < Math.Abs(direction.Y))
        {
            var x = modelLine.GetEndPoint(0).X;
            viewCurve = Line.CreateBound(
                new XYZ(x, bounds.MinYFeet, 0),
                new XYZ(x, bounds.MaxYFeet, 0));
        }
        else
        {
            var y = modelLine.GetEndPoint(0).Y;
            viewCurve = Line.CreateBound(
                new XYZ(bounds.MinXFeet, y, 0),
                new XYZ(bounds.MaxXFeet, y, 0));
        }

        grid.SetCurveInView(DatumExtentType.ViewSpecific, view, viewCurve);
    }
}
