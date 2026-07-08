using Autodesk.Revit.DB;
using Autodesk.Revit.DB;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Services.AnnotationComponents;

public static class DimensionAnnotationHelper
{
    public const double MillimetersToFeet = 1.0 / 304.8;
    public const double DefaultOffsetMm = 304.8;
    public const double DefaultPickToleranceMm = 1524;

    public static double ResolveOffsetMm(double offsetMm)
    {
        return offsetMm > 0 ? offsetMm : DefaultOffsetMm;
    }

    public static XYZ ConvertMmToFeet(double x, double y, double z)
    {
        return new XYZ(x * MillimetersToFeet, y * MillimetersToFeet, z * MillimetersToFeet);
    }

    public static XYZ ConvertMmToFeet(JZPoint point)
    {
        return ConvertMmToFeet(point.X, point.Y, point.Z);
    }

    public static DimensionType ResolveDimensionType(Document doc, string dimensionTypeName, int dimensionStyleId)
    {
        if (dimensionStyleId > 0)
        {
            var byId = doc.GetElement(new ElementId(dimensionStyleId)) as DimensionType;
            if (byId != null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(dimensionTypeName))
        {
            var trimmed = dimensionTypeName.Trim();
            var types = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
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
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .FirstOrDefault(type => type.StyleType == DimensionStyleType.Linear);
    }

    public static void ApplyDimensionType(
        Dimension dimension,
        Document doc,
        string dimensionTypeName,
        int dimensionStyleId)
    {
        if (dimensionStyleId <= 0 && string.IsNullOrWhiteSpace(dimensionTypeName))
            return;

        var dimensionType = ResolveDimensionType(doc, dimensionTypeName, dimensionStyleId);
        if (dimensionType == null || dimensionType.StyleType != DimensionStyleType.Linear)
            return;

        try
        {
            dimension.DimensionType = dimensionType;
        }
        catch
        {
            // Keep the dimension with Revit default type when assignment is incompatible.
        }
    }

    public static Line BuildDimensionLine(XYZ startPoint, XYZ endPoint, JZPoint linePoint, double offsetMm)
    {
        var direction = (endPoint - startPoint).Normalize();
        if (direction.GetLength() < 1e-9)
            direction = XYZ.BasisX;

        var anchor = linePoint != null
            ? ConvertMmToFeet(linePoint)
            : ComputeDefaultLineAnchor(startPoint, endPoint, direction, offsetMm);

        var halfLength = startPoint.DistanceTo(endPoint) / 2.0;
        return Line.CreateBound(
            anchor - direction * halfLength,
            anchor + direction * halfLength);
    }

    public static Reference FindReferenceAtPoint(
        Document doc,
        View view,
        XYZ point,
        XYZ dimensionDirection,
        double pickToleranceMm = DefaultPickToleranceMm)
    {
        var collector = new FilteredElementCollector(doc, view.Id);
        var elements = collector.WhereElementIsNotElementType().ToElements();

        Element closestElement = null;
        var minDistance = double.MaxValue;
        var toleranceFeet = pickToleranceMm * MillimetersToFeet;

        foreach (var element in elements)
        {
            if (element.Location == null)
                continue;

            XYZ elementPoint = null;
            if (element.Location is LocationPoint locationPoint)
            {
                elementPoint = locationPoint.Point;
            }
            else if (element.Location is LocationCurve locationCurve)
            {
                elementPoint = locationCurve.Curve.Project(point).XYZPoint;
            }
            else
            {
                continue;
            }

            var distance = point.DistanceTo(elementPoint);
            if (distance < minDistance)
            {
                closestElement = element;
                minDistance = distance;
            }
        }

        if (closestElement == null || minDistance > toleranceFeet)
            return null;

        var refs = GetReferences(closestElement, view, dimensionDirection);
        return refs.Count > 0 ? refs[0] : null;
    }

    public static Reference GetBestWallReference(Wall wall, View view, XYZ measureDirection, XYZ _)
    {
        var refs = GetReferences(wall, view, measureDirection);
        return refs.Count > 0 ? refs[0] : null;
    }

    public static List<Reference> GetReferences(Element element, View view, XYZ dimensionDirection = null)
    {
        var references = new List<Reference>();

        if (element is Wall wall)
        {
            var options = new Options
            {
                View = view,
                ComputeReferences = true
            };

            var geometry = wall.get_Geometry(options);
            if (geometry != null)
            {
                Reference bestRef = null;
                var bestAlignment = -1.0;

                foreach (var obj in geometry)
                {
                    if (obj is not Solid solid || solid.Faces.Size <= 0)
                        continue;

                    foreach (Face face in solid.Faces)
                    {
                        if (face is not PlanarFace planarFace)
                            continue;

                        var normal = planarFace.FaceNormal;
                        if (Math.Abs(normal.Z) > 0.9)
                            continue;

                        if (dimensionDirection != null)
                        {
                            var alignment = Math.Abs(normal.DotProduct(dimensionDirection));
                            if (alignment > bestAlignment)
                            {
                                bestAlignment = alignment;
                                bestRef = face.Reference;
                            }
                        }
                        else
                        {
                            references.Add(face.Reference);
                            return references;
                        }
                    }
                }

                if (bestRef != null)
                    references.Add(bestRef);
            }

            if (references.Count == 0)
                references.Add(new Reference(wall));
        }
        else if (element is FamilyInstance familyInstance)
        {
            var options = new Options
            {
                View = view,
                ComputeReferences = true
            };

            var geometry = familyInstance.get_Geometry(options);
            if (geometry != null && dimensionDirection != null)
            {
                Reference bestRef = null;
                var bestAlignment = -1.0;

                foreach (var obj in geometry)
                {
                    var solids = new List<Solid>();
                    if (obj is Solid solid && solid.Faces.Size > 0)
                        solids.Add(solid);
                    else if (obj is GeometryInstance geometryInstance)
                    {
                        foreach (var subObj in geometryInstance.GetInstanceGeometry())
                        {
                            if (subObj is Solid subSolid && subSolid.Faces.Size > 0)
                                solids.Add(subSolid);
                        }
                    }

                    foreach (var solidBody in solids)
                    {
                        foreach (Face face in solidBody.Faces)
                        {
                            if (face is not PlanarFace planarFace)
                                continue;

                            if (Math.Abs(planarFace.FaceNormal.Z) > 0.9)
                                continue;

                            var alignment = Math.Abs(planarFace.FaceNormal.DotProduct(dimensionDirection));
                            if (alignment > bestAlignment)
                            {
                                bestAlignment = alignment;
                                bestRef = face.Reference;
                            }
                        }
                    }
                }

                if (bestRef != null)
                {
                    references.Add(bestRef);
                    return references;
                }
            }

            references.Add(new Reference(familyInstance));
        }
        else
        {
            references.Add(new Reference(element));
        }

        return references;
    }

    private static XYZ ComputeDefaultLineAnchor(XYZ startPoint, XYZ endPoint, XYZ direction, double offsetMm)
    {
        var midpoint = (startPoint + endPoint) / 2.0;
        var perpendicular = new XYZ(-direction.Y, direction.X, 0);
        if (perpendicular.GetLength() < 1e-9)
            perpendicular = XYZ.BasisY;
        else
            perpendicular = perpendicular.Normalize();

        return midpoint + perpendicular * (ResolveOffsetMm(offsetMm) * MillimetersToFeet);
    }
}
