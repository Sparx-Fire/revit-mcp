using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitMCPKgCommandSet.Core;

namespace RevitMCPKgCommandSet.Services
{
    public sealed class RevitElementReader : IElementReader
    {
        private const double FeetToMetres = 0.3048;
        private const double SqFeetToSqMetres = FeetToMetres * FeetToMetres;
        // Revit stores lengths in feet internally; * 0.3048 yields the classic
        // double-precision tail (2.7499999999999996, 5.99999999999998, ...).
        // Round at the source so every downstream consumer — projection,
        // drift detect/resolve, kg_v2_query dump — sees clean values. 6
        // decimals in metres = 1 µm precision, well below BIM tolerance.
        private const int MetresRoundDecimals = 6;
        private static double RoundM(double m) => Math.Round(m, MetresRoundDecimals);

        private readonly Document _doc;

        public RevitElementReader(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        public string ResolveNodeType(long elementId)
        {
            var el = GetElement(elementId);
            return ResolveTypeOf(el);
        }

        public Dictionary<string, object> ReadAttrs(long elementId)
        {
            var el = GetElement(elementId);
            if (el == null) return null;
            switch (el)
            {
                case Level lvl: return ReadLevelAttrs(lvl);
                case WallType wt: return ReadWallTypeAttrs(wt);
                case Wall w: return ReadWallAttrs(w);
                case Room room: return ReadRoomAttrs(room);
                case FamilyInstance fi: return IsOpening(fi) ? ReadOpeningAttrs(fi) : null;
                case FamilySymbol fs: return IsOpeningSymbol(fs) ? ReadFamilyTypeAttrs(fs) : null;
                default: return null;
            }
        }

        public IEnumerable<long> EnumerateAllElementIds()
        {
            // Mirrors KgV2DocumentWatcher.BootstrapDocument's scan classes
            // (Level, WallType, FamilySymbol-of-openings, Wall,
            // FamilyInstance-of-openings, Room) so drift detection covers
            // exactly the node-type surface the projection knows about.
            var ids = new List<long>();

            ids.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .WhereElementIsNotElementType()
                .Select(e => SafeIdValue(e.Id))
                .Where(v => v > 0));

            ids.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(WallType))
                .WhereElementIsElementType()
                .Select(e => SafeIdValue(e.Id))
                .Where(v => v > 0));

            ids.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(IsOpeningSymbol)
                .Select(e => SafeIdValue(e.Id))
                .Where(v => v > 0));

            ids.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Select(e => SafeIdValue(e.Id))
                .Where(v => v > 0));

            ids.AddRange(new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(IsOpening)
                .Select(e => SafeIdValue(e.Id))
                .Where(v => v > 0));

            ids.AddRange(new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Select(e => SafeIdValue(e.Id))
                .Where(v => v > 0));

            return ids;
        }

        public IEnumerable<EdgeSpec> ReadEdges(long elementId)
        {
            var el = GetElement(elementId);
            if (el == null) yield break;

            switch (el)
            {
                case Wall w:
                {
                    var lvlId = SafeIdValue(w.LevelId);
                    if (lvlId > 0) yield return new EdgeSpec(lvlId, EdgeTypes.AtLevel);
                    var typeId = SafeIdValue(w.GetTypeId());
                    if (typeId > 0) yield return new EdgeSpec(typeId, EdgeTypes.IsType);
                    break;
                }
                case FamilyInstance fi when IsOpening(fi):
                {
                    var symId = SafeIdValue(fi.GetTypeId());
                    if (symId > 0) yield return new EdgeSpec(symId, EdgeTypes.IsType);

                    var host = fi.Host;
                    if (host is Wall wallHost)
                    {
                        var wallId = SafeIdValue(wallHost.Id);
                        if (wallId > 0)
                            yield return new EdgeSpec(wallId, EdgeTypes.Hosts, EdgeDirection.Incoming);
                    }

                    var lvlId = SafeIdValue(fi.LevelId);
                    if (lvlId > 0) yield return new EdgeSpec(lvlId, EdgeTypes.AtLevel);
                    break;
                }
                case Room room:
                {
                    var lvlId = SafeIdValue(room.LevelId);
                    if (lvlId > 0) yield return new EdgeSpec(lvlId, EdgeTypes.AtLevel);
                    foreach (var wallId in ReadBoundaryWallIds(room))
                        yield return new EdgeSpec(wallId, EdgeTypes.BoundedBy);
                    break;
                }
            }
        }

        // ---- type resolution ----

        private static string ResolveTypeOf(Element el)
        {
            switch (el)
            {
                case null: return null;
                case Level _: return "Level";
                case WallType _: return "WallType";
                case Wall _: return "Wall";
                case Room _: return "Room";
                case FamilyInstance fi:
                    if (IsCategory(fi, BuiltInCategory.OST_Windows)) return "Window";
                    if (IsCategory(fi, BuiltInCategory.OST_Doors)) return "Door";
                    return null;
                case FamilySymbol fs:
                    return IsOpeningSymbol(fs) ? "FamilyType" : null;
                default:
                    return null;
            }
        }

        private static bool IsOpening(FamilyInstance fi) =>
            IsCategory(fi, BuiltInCategory.OST_Windows) || IsCategory(fi, BuiltInCategory.OST_Doors);

        private static bool IsOpeningSymbol(FamilySymbol fs) =>
            IsCategory(fs, BuiltInCategory.OST_Windows) || IsCategory(fs, BuiltInCategory.OST_Doors);

        private static bool IsCategory(Element el, BuiltInCategory bic)
        {
            var cat = el?.Category;
            if (cat == null) return false;
            return SafeIdValue(cat.Id) == (long)bic;
        }

        // ---- attribute readers ----

        private static Dictionary<string, object> ReadLevelAttrs(Level lvl) =>
            new Dictionary<string, object>
            {
                ["name"] = lvl.Name ?? string.Empty,
                ["elevation"] = RoundM(lvl.Elevation * FeetToMetres),
            };

        private static Dictionary<string, object> ReadWallTypeAttrs(WallType wt) =>
            new Dictionary<string, object>
            {
                ["name"] = wt.Name ?? string.Empty,
                ["total_thickness"] = RoundM(wt.Width * FeetToMetres),
            };

        private Dictionary<string, object> ReadWallAttrs(Wall w)
        {
            double[] p1 = null, p2 = null;
            double length = 0;
            if (w.Location is LocationCurve lc && lc.Curve != null)
            {
                var a = lc.Curve.GetEndPoint(0);
                var b = lc.Curve.GetEndPoint(1);
                p1 = new[] { RoundM(a.X * FeetToMetres), RoundM(a.Y * FeetToMetres) };
                p2 = new[] { RoundM(b.X * FeetToMetres), RoundM(b.Y * FeetToMetres) };
                length = RoundM(lc.Curve.ApproximateLength * FeetToMetres);
            }
            double height = TryGetParamMetres(w, BuiltInParameter.WALL_USER_HEIGHT_PARAM) ?? 0.0;

            return new Dictionary<string, object>
            {
                ["type_ref"] = $"revit_{SafeIdValue(w.GetTypeId())}",
                ["level_ref"] = $"revit_{SafeIdValue(w.LevelId)}",
                ["p1"] = p1 ?? new[] { 0.0, 0.0 },
                ["p2"] = p2 ?? new[] { 0.0, 0.0 },
                ["length"] = length,
                ["height"] = height,
            };
        }

        private Dictionary<string, object> ReadOpeningAttrs(FamilyInstance fi)
        {
            double[] position = null;
            if (fi.Location is LocationPoint lp && lp.Point != null)
            {
                position = new[] { RoundM(lp.Point.X * FeetToMetres), RoundM(lp.Point.Y * FeetToMetres) };
            }
            double sill = TryGetParamMetres(fi, BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM) ?? 0.0;
            double head = TryGetParamMetres(fi, BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM) ?? 0.0;
            long hostId = fi.Host is Wall w ? SafeIdValue(w.Id) : 0;

            return new Dictionary<string, object>
            {
                ["type_ref"] = $"revit_{SafeIdValue(fi.GetTypeId())}",
                ["host_wall_ref"] = $"revit_{hostId}",
                ["position"] = position ?? new[] { 0.0, 0.0 },
                ["sill_height"] = sill,
                ["head_height"] = head,
            };
        }

        private static Dictionary<string, object> ReadFamilyTypeAttrs(FamilySymbol fs) =>
            new Dictionary<string, object>
            {
                ["family_name"] = fs.Family?.Name ?? string.Empty,
                ["type_name"] = fs.Name ?? string.Empty,
                ["category"] = fs.Category?.Name ?? string.Empty,
            };

        private Dictionary<string, object> ReadRoomAttrs(Room room)
        {
            // Room.Name on its own can fold in the number ("Bureau 12");
            // ROOM_NAME isolates the user-facing name, with a defensive
            // fallback to Element.Name.
            var name = TryGetParamString(room, BuiltInParameter.ROOM_NAME);
            if (string.IsNullOrEmpty(name)) name = room.Name ?? string.Empty;

            var attrs = new Dictionary<string, object>
            {
                ["name"] = name,
                ["level_ref"] = $"revit_{SafeIdValue(room.LevelId)}",
                // Area is stored in square feet internally; metres² to match
                // every other KG v2 spatial attr. 0 for unplaced/unenclosed.
                ["area"] = RoundM(room.Area * SqFeetToSqMetres),
            };

            var boundaryWalls = ReadBoundaryWallIds(room)
                .Select(id => $"revit_{id}")
                .ToList();
            if (boundaryWalls.Count > 0) attrs["boundary_walls"] = boundaryWalls;

            return attrs;
        }

        // Distinct Wall ElementIds that bound the room (Finish location).
        // Non-wall boundary segments (separation lines, other rooms) and
        // unenclosed rooms yield nothing. Not an iterator so the
        // GetBoundarySegments call can sit inside a try/catch.
        private List<long> ReadBoundaryWallIds(Room room)
        {
            var result = new List<long>();
            var seen = new HashSet<long>();
            IList<IList<BoundarySegment>> loops;
            try { loops = room.GetBoundarySegments(new SpatialElementBoundaryOptions()); }
            catch { return result; }
            if (loops == null) return result;

            foreach (var loop in loops)
            {
                if (loop == null) continue;
                foreach (var seg in loop)
                {
                    if (seg == null) continue;
                    var id = SafeIdValue(seg.ElementId);
                    if (id <= 0 || !seen.Add(id)) continue;
                    if (GetElement(id) is Wall) result.Add(id);
                }
            }
            return result;
        }

        // ---- helpers ----

        private Element GetElement(long elementId)
        {
            try { return _doc.GetElement(new ElementId(elementId)); }
            catch { return null; }
        }

        private static long SafeIdValue(ElementId id)
        {
            if (id == null) return 0;
            try { return id.Value; }
            catch { return 0; }
        }

        private static double? TryGetParamMetres(Element el, BuiltInParameter bip)
        {
            try
            {
                var p = el.get_Parameter(bip);
                if (p == null || !p.HasValue) return null;
                return RoundM(p.AsDouble() * FeetToMetres);
            }
            catch { return null; }
        }

        private static string TryGetParamString(Element el, BuiltInParameter bip)
        {
            try
            {
                var p = el.get_Parameter(bip);
                return p != null && p.HasValue ? p.AsString() : null;
            }
            catch { return null; }
        }
    }
}
