using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;

namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// 跨版本兼容性扩展方法
    /// 封装 Revit R20-R26 之间的 API 差异
    /// </summary>
    public static class VersionCompat
    {
        public static Curve GetWallLocationCurve(Wall wall)
        {
            return (wall.Location as LocationCurve)?.Curve;
        }

        // ===== RevisionCloud.Create 兼容 =====
        // R20-R22: Create(Doc, View, ElemId, IList{Curve})
        // R23-R24: Create(Doc, ElemId, IList{CurveLoop}, View)
        // R25:     Create(Doc, ElemId, IList{CurveLoop}, ElemId)
        // R26:     back to Create(Doc, View, ElemId, IList{Curve})
        public static RevisionCloud CreateRevisionCloud(
            Document doc, ElementId revisionId, IList<CurveLoop> loops, ElementId viewId)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            var curves = new List<Curve>();
            foreach (var loop in loops)
                foreach (var curve in loop)
                    curves.Add(curve);
            View view = doc.GetElement(viewId) as View;
            return RevisionCloud.Create(doc, view, revisionId, curves);
#else
            return RevisionCloud.Create(doc, revisionId, loops, viewId);
#endif
#else
            // R20-R24, R26: Create(Doc, View, ElemId, IList{Curve})
            var curves = new List<Curve>();
            foreach (var loop in loops)
                foreach (var curve in loop)
                    curves.Add(curve);
            View view = doc.GetElement(viewId) as View;
            return RevisionCloud.Create(doc, view, revisionId, curves);
#endif
        }

        public static void SetRevisionNumber(this Revision revision, string number)
        {
            revision.get_Parameter(BuiltInParameter.PROJECT_REVISION_REVISION_NUM).Set(number);
        }

        // ===== ReferencePlane.Create 兼容 =====
        public static ReferencePlane CreateReferencePlane(Document doc, Plane plane)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return doc.Create.NewReferencePlane(plane.Origin, plane.Origin + plane.XVec, plane.Normal, doc.ActiveView);
#else
            return ReferencePlane.Create(doc, plane);
#endif
#else
            return doc.Create.NewReferencePlane(plane.Origin, plane.Origin + plane.XVec, plane.Normal, doc.ActiveView);
#endif
        }

        // ===== TextNote.Rotation 兼容 =====
        public static void SetTextNoteRotation(Document doc, TextNote textNote, double rotation)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            XYZ center = textNote.Coord;
            Line axis = Line.CreateUnbound(center, XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, textNote.Id, axis, rotation);
#else
            textNote.Rotation = rotation;
#endif
#else
            XYZ center = textNote.Coord;
            Line axis = Line.CreateUnbound(center, XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, textNote.Id, axis, rotation);
#endif
        }

        // ===== ViewSchedule 属性兼容 (R25+ only) =====
        public static void SetScheduleShowHeaders(ViewSchedule schedule, bool show)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            Parameter p = schedule.LookupParameter("Show Headers");
            if (p != null) p.Set(show ? 1 : 0);
#else
            schedule.ShowHeaders = show;
#endif
#endif
        }
        public static void SetScheduleShowGridLines(ViewSchedule schedule, bool show)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            Parameter p = schedule.LookupParameter("Show Grid Lines");
            if (p != null) p.Set(show ? 1 : 0);
#else
            schedule.ShowGridLines = show;
#endif
#endif
        }
        public static void SetScheduleShowOutlines(ViewSchedule schedule, bool show)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            Parameter p = schedule.LookupParameter("Show Outlines");
            if (p != null) p.Set(show ? 1 : 0);
#else
            schedule.ShowOutlines = show;
#endif
#endif
        }

        // ===== NurbsSpline (R25+ NurbSpline.Create(IList{XYZ}), R20-R24: HermiteSpline) =====
        public static Curve CreateNurbSpline(IList<XYZ> points)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            var hermiteSpline = HermiteSpline.Create(points, false);
            return NurbSpline.Create(hermiteSpline);
#else
            return NurbSpline.Create(new List<XYZ>(points));
#endif
#else
            var hermiteSpline = HermiteSpline.Create(points, false);
            return NurbSpline.Create(hermiteSpline);
#endif
        }

        // ===== ViewDuplicateOption.Dependent (R25+) =====
        public static ViewDuplicateOption GetDependentDuplicateOption()
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return ViewDuplicateOption.Duplicate;
#else
            return ViewDuplicateOption.Dependent;
#endif
#else
            return ViewDuplicateOption.Duplicate;
#endif
        }

        // ===== Ceiling.Create (R23+) =====
        public static Ceiling CreateCeiling(Document doc, IList<CurveLoop> profile, ElementId ceilingTypeId, ElementId levelId)
        {
#if REVIT2023_OR_GREATER
            return Ceiling.Create(doc, profile, ceilingTypeId, levelId);
#else
            return null;
#endif
        }

        // ===== Floor.Create (R23+) =====
        public static Floor CreateFloor(Document doc, IList<CurveLoop> profile, ElementId floorTypeId, ElementId levelId)
        {
#if REVIT2023_OR_GREATER
            return Floor.Create(doc, profile, floorTypeId, levelId);
#else
            return null;
#endif
        }

        // ===== Space.Create (R25+) =====
        public static Space CreateSpace(Document doc, ElementId levelId, XYZ point)
        {
#if REVIT2026_OR_GREATER
            Level level = doc.GetElement(levelId) as Level;
            if (level == null) return null;
            return doc.Create.NewSpace(level, new UV(point.X, point.Y));
#elif REVIT2025_OR_GREATER
            return Space.Create(doc, levelId, point);
#else
            return null;
#endif
        }

        // ===== MEPSystem.AddElements (R25+) =====
        public static void AddElementsToMEPSystem(MEPSystem system, IList<ElementId> elementIds)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
#else
            system.AddElements(elementIds);
#endif
#endif
        }

        // ===== ElevationMarker (R25+) =====
        public static ViewSection CreateElevationView(ElevationMarker marker, ElementId levelId, int index)
        {
#if REVIT2026_OR_GREATER
            return null;
#elif REVIT2025_OR_GREATER
            return marker.CreateElevationView(levelId, index);
#else
            return null;
#endif
        }

        // ===== ViewSection.CreateCallout (R25+) =====
        public static ViewSection CreateCallout(Document doc, ElementId hostViewId, ElementId viewFamilyTypeId, BoundingBoxXYZ box)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return null;
#else
            return ViewSection.CreateCallout(doc, hostViewId, viewFamilyTypeId, box);
#endif
#else
            return null;
#endif
        }

        // ===== View.CreateViewTemplate (R25+) =====
        public static ElementId CreateViewTemplate(Document doc, ElementId sourceViewId)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return ElementId.InvalidElementId;
#else
            return View.CreateViewTemplate(doc, sourceViewId);
#endif
#else
            return ElementId.InvalidElementId;
#endif
        }

        // ===== Duct.Create (R25+) =====
        public static Duct CreateDuct(Document doc, ElementId systemTypeId, XYZ start, XYZ end, ElementId levelId)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return null;
#else
            return Duct.Create(doc, systemTypeId, start, end, levelId);
#endif
#else
            return null;
#endif
        }

        // ===== Pipe.Create (R25+) =====
        public static Pipe CreatePipe(Document doc, ElementId systemTypeId, XYZ start, XYZ end, ElementId levelId)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return null;
#else
            return Pipe.Create(doc, systemTypeId, start, end, levelId);
#endif
#else
            return null;
#endif
        }

        // ===== Conduit.Create (R25+) =====
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
        public static MEPCurve CreateConduit(Document doc, ElementId conduitTypeId, XYZ start, XYZ end, ElementId levelId) => null;
#else
        public static Conduit CreateConduit(Document doc, ElementId conduitTypeId, XYZ start, XYZ end, ElementId levelId)
            => Conduit.Create(doc, conduitTypeId, start, end, levelId);
#endif
#else
        public static object CreateConduit(Document doc, ElementId conduitTypeId, XYZ start, XYZ end, ElementId levelId) => null;
#endif

        // ===== Category.BuiltInCategory (R25+) =====
        public static BuiltInCategory GetBuiltInCategory(Category category)
        {
#if REVIT2025_OR_GREATER
            return category.BuiltInCategory;
#else
            return (BuiltInCategory)category.Id.IntegerValue;
#endif
        }

        // ===== ViewSheet.AddRevision (R25+) =====
        public static void AddRevisionToSheet(ViewSheet sheet, ElementId revisionId)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
#else
            sheet.AddRevision(revisionId);
#endif
#endif
        }

        // ===== ScheduleDefinition (R25+) =====
        public static void SetScheduleFieldVisibility(ScheduleDefinition definition, ScheduleFieldId fieldId, bool visible)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
#else
            definition.SetFieldVisibility(fieldId, visible);
#endif
#endif
        }
        public static ElementId GetScheduleCategoryId(ScheduleDefinition definition)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return ElementId.InvalidElementId;
#else
            return definition.GetCategoryId();
#endif
#else
            return ElementId.InvalidElementId;
#endif
        }
        // Category.Parameters (R25+)
        public static IEnumerable<Parameter> GetCategoryParameters(Category category)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return Enumerable.Empty<Parameter>();
#else
            return category.Parameters?.Cast<Parameter>() ?? Enumerable.Empty<Parameter>();
#endif
#else
            return Enumerable.Empty<Parameter>();
#endif
        }

        // ===== Face.SurfaceType (R25+) =====
        public static string GetSurfaceTypeName(Face face)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            if (face is PlanarFace) return "Planar";
            if (face is CylindricalFace) return "Cylindrical";
            if (face is RevolvedFace) return "Revolved";
            if (face is RuledFace) return "Ruled";
            if (face is HermiteFace) return "Hermite";
            return "Unknown";
#else
            return face.SurfaceType.ToString();
#endif
#else
            if (face is PlanarFace) return "Planar";
            if (face is CylindricalFace) return "Cylindrical";
            if (face is RevolvedFace) return "Revolved";
            if (face is RuledFace) return "Ruled";
            if (face is HermiteFace) return "Hermite";
            return "Unknown";
#endif
        }

        // ===== IntersectionResult.Reference (R25+) =====
        public static Reference GetIntersectionReference(IntersectionResult result)
        {
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
            return null;
#else
            return result.Reference;
#endif
#else
            return null;
#endif
        }

        // ===== DisplayStyle 兼容 =====
        public static string GetDisplayStyleName(string styleName)
        {
            switch (styleName.ToLower())
            {
                case "wireframe": return "Wireframe";
                case "hidden":
                case "hiddenline": return "HiddenLine";
                case "shaded":
                case "shading": return "Shading";
                case "consistent_colors": return "ConsistentColors";
                case "realistic": return "Realistic";
                default: return "HiddenLine";
            }
        }
    }
}
