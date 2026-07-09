using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.AnnotationComponents;

public class DimensionRoomWallsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private RoomWallDimensionInfo _info;
    private readonly ManualResetEvent _resetEvent = new(false);

    public AIResult<List<int>> Result { get; private set; }

    public void SetParameters(RoomWallDimensionInfo info)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _resetEvent.Reset();
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument
                ?? throw new InvalidOperationException("No active Revit document.");

            var doc = uiDoc.Document;
            var view = ResolveView(doc, uiDoc, _info.ViewId);
            if (view is not ViewPlan viewPlan)
                throw new InvalidOperationException("Room wall dimensions require an active floor plan view.");

            var room = doc.GetElement(new ElementId(_info.RoomId)) as Room
                ?? throw new InvalidOperationException($"Room '{_info.RoomId}' was not found.");

            if (room.Area <= 0)
                throw new InvalidOperationException("Room is not placed or has zero area.");

            var boundaryOptions = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            var loops = room.GetBoundarySegments(boundaryOptions);
            if (loops == null || loops.Count == 0)
                throw new InvalidOperationException("Room has no boundary segments.");

            var edges = CollectBoundaryEdges(doc, loops);
            if (edges.Count == 0)
                throw new InvalidOperationException("Room boundary has no wall segments to dimension.");

            var bounds = ComputeBounds(edges);
            var roomCenter = GetRoomCenter(room, bounds);
            var offsetFeet = DimensionAnnotationHelper.ResolveOffsetMm(_info.OffsetMm)
                * DimensionAnnotationHelper.MillimetersToFeet;

            var createdDimensionIds = new List<int>();
            using (var transaction = new Transaction(doc, "Dimension Room Walls"))
            {
                transaction.Start();

                var xChain = CreateChainDimension(
                    doc,
                    viewPlan,
                    CollectChainReferences(edges, viewPlan, forXChain: true),
                    forXChain: true,
                    bounds,
                    roomCenter,
                    offsetFeet);
                if (xChain != null)
                    createdDimensionIds.Add(xChain.Id.GetIntValue());

                var yChain = CreateChainDimension(
                    doc,
                    viewPlan,
                    CollectChainReferences(edges, viewPlan, forXChain: false),
                    forXChain: false,
                    bounds,
                    roomCenter,
                    offsetFeet);
                if (yChain != null)
                    createdDimensionIds.Add(yChain.Id.GetIntValue());

                transaction.Commit();
            }

            if (createdDimensionIds.Count == 0)
                throw new InvalidOperationException("No room wall dimensions could be created.");

            Result = new AIResult<List<int>>
            {
                Success = true,
                Message =
                    $"Successfully created {createdDimensionIds.Count} room wall dimension chain(s).",
                Response = createdDimensionIds
            };
        }
        catch (Exception ex)
        {
            Result = new AIResult<List<int>>
            {
                Success = false,
                Message = $"Error creating room wall dimensions: {ex.Message}",
                Response = new List<int>()
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName() => "Dimension Room Walls";

    private static View ResolveView(Document doc, UIDocument uiDoc, int viewId)
    {
        if (viewId > 0)
        {
            var view = doc.GetElement(new ElementId(viewId)) as View;
            if (view != null)
                return view;
        }

        return uiDoc.ActiveView;
    }

    private sealed class BoundaryEdge
    {
        public Curve Curve { get; set; }
        public Wall Wall { get; set; }
        public bool IsHorizontal { get; set; }
    }

    private sealed class Bounds
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
    }

    private static List<BoundaryEdge> CollectBoundaryEdges(
        Document doc,
        IList<IList<BoundarySegment>> loops)
    {
        var edges = new List<BoundaryEdge>();

        foreach (var loop in loops)
        {
            foreach (BoundarySegment segment in loop)
            {
                var wall = doc.GetElement(segment.ElementId) as Wall;
                if (wall == null)
                    continue;

                var curve = segment.GetCurve();
                if (curve == null)
                    continue;

                var delta = curve.GetEndPoint(1) - curve.GetEndPoint(0);
                if (delta.GetLength() < 1e-6)
                    continue;

                edges.Add(new BoundaryEdge
                {
                    Curve = curve,
                    Wall = wall,
                    IsHorizontal = Math.Abs(delta.X) >= Math.Abs(delta.Y)
                });
            }
        }

        return edges;
    }

    private static Bounds ComputeBounds(List<BoundaryEdge> edges)
    {
        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var minY = double.MaxValue;
        var maxY = double.MinValue;

        foreach (var edge in edges)
        {
            foreach (var point in new[] { edge.Curve.GetEndPoint(0), edge.Curve.GetEndPoint(1) })
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }
        }

        return new Bounds
        {
            MinX = minX,
            MaxX = maxX,
            MinY = minY,
            MaxY = maxY
        };
    }

    private static XYZ GetRoomCenter(Room room, Bounds bounds)
    {
        if (room.Location is LocationPoint locationPoint)
            return locationPoint.Point;

        return new XYZ(
            (bounds.MinX + bounds.MaxX) / 2.0,
            (bounds.MinY + bounds.MaxY) / 2.0,
            0);
    }

    private static List<(double Key, Reference Ref)> CollectChainReferences(
        List<BoundaryEdge> edges,
        View view,
        bool forXChain)
    {
        var references = new List<(double Key, Reference Ref)>();
        var measureDirection = forXChain ? XYZ.BasisX : XYZ.BasisY;

        foreach (var edge in edges)
        {
            if (forXChain && edge.IsHorizontal)
                continue;
            if (!forXChain && !edge.IsHorizontal)
                continue;

            var start = edge.Curve.GetEndPoint(0);
            var end = edge.Curve.GetEndPoint(1);

            var startRef = DimensionAnnotationHelper.GetBestWallReference(
                edge.Wall,
                view,
                measureDirection,
                start);
            var endRef = DimensionAnnotationHelper.GetBestWallReference(
                edge.Wall,
                view,
                measureDirection,
                end);

            if (startRef != null)
                TryAddReference(references, forXChain ? start.X : start.Y, startRef);
            if (endRef != null)
                TryAddReference(references, forXChain ? end.X : end.Y, endRef);
        }

        return references;
    }

    private static void TryAddReference(List<(double Key, Reference Ref)> references, double key, Reference reference)
    {
        const double toleranceFeet = 1.0 * DimensionAnnotationHelper.MillimetersToFeet;
        if (references.Any(existing => Math.Abs(existing.Key - key) <= toleranceFeet))
            return;

        references.Add((key, reference));
    }

    private Dimension CreateChainDimension(
        Document doc,
        ViewPlan view,
        List<(double Key, Reference Ref)> references,
        bool forXChain,
        Bounds bounds,
        XYZ roomCenter,
        double offsetFeet)
    {
        if (references.Count < 2)
            return null;

        var sorted = references.OrderBy(item => item.Key).ToList();
        var referenceArray = new ReferenceArray();
        foreach (var item in sorted)
            referenceArray.Append(item.Ref);

        var z = view.GenLevel?.Elevation ?? 0.0;
        var extension = Math.Max(bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY) * 0.1 + 1.0;
        Line line;

        if (forXChain)
        {
            var y = roomCenter.Y >= (bounds.MinY + bounds.MaxY) / 2.0
                ? bounds.MinY - offsetFeet
                : bounds.MaxY + offsetFeet;
            line = Line.CreateBound(
                new XYZ(bounds.MinX - extension, y, z),
                new XYZ(bounds.MaxX + extension, y, z));
        }
        else
        {
            var x = roomCenter.X >= (bounds.MinX + bounds.MaxX) / 2.0
                ? bounds.MinX - offsetFeet
                : bounds.MaxX + offsetFeet;
            line = Line.CreateBound(
                new XYZ(x, bounds.MinY - extension, z),
                new XYZ(x, bounds.MaxY + extension, z));
        }

        var dimension = doc.Create.NewDimension(view, line, referenceArray);
        DimensionAnnotationHelper.ApplyDimensionType(
            dimension,
            doc,
            _info.DimensionType,
            _info.DimensionStyleId);
        return dimension;
    }
}
