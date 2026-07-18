using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RevitMCPCommandSet.Services
{
    public class AIElementFilterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;
        /// <summary>
        /// </summary>
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        /// <summary>
        /// </summary>
        public FilterSetting FilterSetting { get; private set; }
        /// <summary>
        /// </summary>
        public AIResult<List<object>> Result { get; private set; }

        /// <summary>
        /// </summary>
        public void SetParameters(FilterSetting data)
        {
            FilterSetting = data;
            _resetEvent.Reset();
        }
        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var elementInfoList = new List<object>();
                // Validate the filter settings
                if (!FilterSetting.Validate(out string errorMessage))
                    throw new Exception(errorMessage);
                // Get the IDs of elements matching the criteria
                var elementList = GetFilteredElements(doc, FilterSetting);
                if (elementList == null || !elementList.Any())
                    throw new Exception("No matching elements found in the project; check the filter settings");
                // Maximum number of elements returned by the filter
                string message = "";
                if (FilterSetting.MaxElements > 0)
                {
                    if (elementList.Count > FilterSetting.MaxElements)
                    {
                        elementList = elementList.Take(FilterSetting.MaxElements).ToList();
                        message = $". In addition, {elementList.Count} elements match the filter; only the first {FilterSetting.MaxElements} are shown";
                    }
                }

                // Get info for elements with the specified IDs
                elementInfoList = GetElementFullInfo(doc, elementList);

                Result = new AIResult<List<object>>
                {
                    Success = true,
                    Message = $"Retrieved {elementInfoList.Count} element(s); details are stored in the Response property" + message,
                    Response = elementInfoList,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<object>>
                {
                    Success = false,
                    Message = $"Error getting element info: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set(); // Notify waiting threads that the operation completed
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="timeoutMilliseconds">Timeout in milliseconds</param>
        /// <returns>Whether the operation completed before the timeout</returns>
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        /// <summary>
        /// </summary>
        public string GetName()
        {
            return "Get Element Info";
        }

        /// <summary>
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <param name="settings">Filter settings</param>
        /// <returns>Elements matching all filter criteria</returns>
        public static IList<Element> GetFilteredElements(Document doc, FilterSetting settings)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            // Validate filter settings
            if (!settings.Validate(out string errorMessage))
            {
                System.Diagnostics.Trace.WriteLine($"Invalid filter settings: {errorMessage}");
                return new List<Element>();
            }
            // Track which filters were applied
            List<string> appliedFilters = new List<string>();
            List<Element> result = new List<Element>();
            // If both types and instances are requested, filter separately and merge
            if (settings.IncludeTypes && settings.IncludeInstances)
            {
                // Collect type elements
                result.AddRange(GetElementsByKind(doc, settings, true, appliedFilters));

                // Collect instance elements
                result.AddRange(GetElementsByKind(doc, settings, false, appliedFilters));
            }
            else if (settings.IncludeInstances)
            {
                // Instances only
                result = GetElementsByKind(doc, settings, false, appliedFilters);
            }
            else if (settings.IncludeTypes)
            {
                // Types only
                result = GetElementsByKind(doc, settings, true, appliedFilters);
            }

            // Report the applied filters
            if (appliedFilters.Count > 0)
            {
                System.Diagnostics.Trace.WriteLine($"Applied {appliedFilters.Count} filter(s): {string.Join(", ", appliedFilters)}");
                System.Diagnostics.Trace.WriteLine($"Final result: {result.Count} element(s) found");
            }
            return result;

        }

        /// <summary>
        /// </summary>
        private static List<Element> GetElementsByKind(Document doc, FilterSetting settings, bool isElementType, List<string> appliedFilters)
        {
            // Create the base FilteredElementCollector
            FilteredElementCollector collector;
            // Optionally restrict to elements visible in the current view (instances only)
            if (!isElementType && settings.FilterVisibleInCurrentView && doc.ActiveView != null)
            {
                collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
                appliedFilters.Add("Visible in current view");
            }
            else
            {
                collector = new FilteredElementCollector(doc);
            }
            // Filter by element kind
            if (isElementType)
            {
                collector = collector.WhereElementIsElementType();
                appliedFilters.Add("Element types only");
            }
            else
            {
                collector = collector.WhereElementIsNotElementType();
                appliedFilters.Add("Element instances only");
            }
            // Build the filter list
            List<ElementFilter> filters = new List<ElementFilter>();
            // 1. Category filter
            if (!string.IsNullOrWhiteSpace(settings.FilterCategory))
            {
                BuiltInCategory category;
                if (!Enum.TryParse(settings.FilterCategory, true, out category))
                {
                    throw new ArgumentException($"Cannot convert '{settings.FilterCategory}' to a valid Revit category.");
                }
                ElementCategoryFilter categoryFilter = new ElementCategoryFilter(category);
                filters.Add(categoryFilter);
                appliedFilters.Add($"Category: {settings.FilterCategory}");
            }
            // 2. Element type filter
            if (!string.IsNullOrWhiteSpace(settings.FilterElementType))
            {

                Type elementType = null;
                // Try several possible forms of the type name
                string[] possibleTypeNames = new string[]
                {
                    settings.FilterElementType,                                    // Raw input
                    $"Autodesk.Revit.DB.{settings.FilterElementType}, RevitAPI",  // Revit API namespace
                    $"{settings.FilterElementType}, RevitAPI"                      // Fully qualified with assembly
                };
                foreach (string typeName in possibleTypeNames)
                {
                    elementType = Type.GetType(typeName);
                    if (elementType != null)
                        break;
                }
                if (elementType != null)
                {
                    ElementClassFilter classFilter = new ElementClassFilter(elementType);
                    filters.Add(classFilter);
                    appliedFilters.Add($"Element type: {elementType.Name}");
                }
                else
                {
                    throw new Exception($"Warning: cannot find type '{settings.FilterElementType}'");
                }
            }
            // 3. Family symbol filter (instances only)
            if (!isElementType && settings.FilterFamilySymbolId > 0)
            {
                ElementId symbolId = new ElementId(settings.FilterFamilySymbolId);
                // Check the element exists and is a family symbol
                Element symbolElement = doc.GetElement(symbolId);
                if (symbolElement != null && symbolElement is FamilySymbol)
                {
                    FamilyInstanceFilter familyFilter = new FamilyInstanceFilter(doc, symbolId);
                    filters.Add(familyFilter);
                    // Log more detailed family info
                    FamilySymbol symbol = symbolElement as FamilySymbol;
                    string familyName = symbol.Family?.Name ?? "Unknown family";
                    string symbolName = symbol.Name ?? "Unknown type";
                    appliedFilters.Add($"Family type: {familyName} - {symbolName} (ID: {settings.FilterFamilySymbolId})");
                }
                else
                {
                    string elementType = symbolElement != null ? symbolElement.GetType().Name : "not found";
                    System.Diagnostics.Trace.WriteLine($"Warning: element with ID {settings.FilterFamilySymbolId} {(symbolElement == null ? "does not exist" : "is not a valid FamilySymbol")} (actual type: {elementType})");
                }
            }
            // 4. Bounding box filter
            if (settings.BoundingBoxMin != null && settings.BoundingBoxMax != null)
            {
                // Convert to Revit XYZ (mm to internal units)
                XYZ minXYZ = JZPoint.ToXYZ(settings.BoundingBoxMin);
                XYZ maxXYZ = JZPoint.ToXYZ(settings.BoundingBoxMax);
                // Create the Outline for the bounding box
                Outline outline = new Outline(minXYZ, maxXYZ);
                // Create the intersection filter
                BoundingBoxIntersectsFilter boundingBoxFilter = new BoundingBoxIntersectsFilter(outline);
                filters.Add(boundingBoxFilter);
                appliedFilters.Add($"Bounding box: Min({settings.BoundingBoxMin.X:F2}, {settings.BoundingBoxMin.Y:F2}, {settings.BoundingBoxMin.Z:F2}), " +
                                  $"Max({settings.BoundingBoxMax.X:F2}, {settings.BoundingBoxMax.Y:F2}, {settings.BoundingBoxMax.Z:F2}) mm");
            }
            // Apply the combined filter
            if (filters.Count > 0)
            {
                ElementFilter combinedFilter = filters.Count == 1
                    ? filters[0]
                    : new LogicalAndFilter(filters);
                collector = collector.WherePasses(combinedFilter);
                if (filters.Count > 1)
                {
                    System.Diagnostics.Trace.WriteLine($"Applied a combined filter with {filters.Count} condition(s) (logical AND)");
                }
            }
            return collector.ToElements().ToList();
        }

        /// <summary>
        /// </summary>
        public static List<object> GetElementFullInfo(Document doc, IList<Element> elementCollector)
        {
            List<object> infoList = new List<object>();

            // Retrieve and process elements
            foreach (var element in elementCollector)
            {
                // Check whether it is a solid model element
                // Get element instance info
                if (element?.Category?.HasMaterialQuantities ?? false)
                {
                    var info = CreateElementFullInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // Get element type info
                else if (element is ElementType elementType)
                {
                    var info = CreateTypeFullInfo(doc, elementType);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // 3. Datum elements (levels/grids) (common)
                else if (element is Level || element is Grid)
                {
                    var info = CreatePositioningElementInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // 4. Spatial elements (fairly common)
                else if (element is SpatialElement) // Room, Area, etc.
                {
                    var info = CreateSpatialElementInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // 5. View elements (common)
                else if (element is View)
                {
                    var info = CreateViewInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // 6. Annotation elements (moderately common)
                else if (element is TextNote || element is Dimension ||
                         element is IndependentTag || element is AnnotationSymbol ||
                         element is SpotDimension)
                {
                    var info = CreateAnnotationInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // 7. Groups and links
                else if (element is Group || element is RevitLinkInstance)
                {
                    var info = CreateGroupOrLinkInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
                // 8. Basic element info (fallback)
                else
                {
                    var info = CreateElementBasicInfo(doc, element);
                    if (info != null)
                    {
                        infoList.Add(info);
                    }
                }
            }

            return infoList;
        }

        /// <summary>
        /// </summary>
        public static ElementInstanceInfo CreateElementFullInfo(Document doc, Element element)
        {
            try
            {
                if (element?.Category == null)
                    return null;

                ElementInstanceInfo elementInfo = new ElementInstanceInfo();        // Custom container for the element's full info
                // ID
                elementInfo.Id = element.Id.GetIntValue();
                // UniqueId
                elementInfo.UniqueId = element.UniqueId;
                // Type name
                elementInfo.Name = element.Name;
                // Family name
                elementInfo.FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString();
                // Category
                elementInfo.Category = element.Category.Name;
                // Built-in category
                elementInfo.BuiltInCategory = Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue());
                // Type ID
                elementInfo.TypeId = element.GetTypeId().GetIntValue();
                // Owning room ID
                if (element is FamilyInstance instance)
                    elementInfo.RoomId = instance.Room?.Id.GetIntValue() ?? -1;
                // Level
                elementInfo.Level = GetElementLevel(doc, element);
                // Overall bounding box
                BoundingBoxInfo boundingBoxInfo = new BoundingBoxInfo();
                elementInfo.BoundingBox = GetBoundingBoxInfo(element);
                //elementInfo.Parameters = GetDimensionParameters(element);
                ParameterInfo thicknessParam = GetThicknessInfo(element);      // Thickness parameter
                if (thicknessParam != null)
                {
                    elementInfo.Parameters.Add(thicknessParam);
                }
                ParameterInfo heightParam = GetBoundingBoxHeight(elementInfo.BoundingBox);      // Height parameter
                if (heightParam != null)
                {
                    elementInfo.Parameters.Add(heightParam);
                }

                return elementInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public static ElementTypeInfo CreateTypeFullInfo(Document doc, ElementType elementType)
        {
            ElementTypeInfo typeInfo = new ElementTypeInfo();
            // Id
            typeInfo.Id = elementType.Id.GetIntValue();
            // UniqueId
            typeInfo.UniqueId = elementType.UniqueId;
            // Type name
            typeInfo.Name = elementType.Name;
            // Family name
            typeInfo.FamilyName = elementType.FamilyName;
            // Category
            typeInfo.Category = elementType.Category.Name;
            // Built-in category
            typeInfo.BuiltInCategory = Enum.GetName(typeof(BuiltInCategory), elementType.Category.Id.GetIntValue());
            // Parameter dictionary
            typeInfo.Parameters = GetDimensionParameters(elementType);
            ParameterInfo thicknessParam = GetThicknessInfo(elementType);      // Thickness parameter
            if (thicknessParam != null)
            {
                typeInfo.Parameters.Add(thicknessParam);
            }
            return typeInfo;
        }

        /// <summary>
        /// </summary>
        public static PositioningElementInfo CreatePositioningElementInfo(Document doc, Element element)
        {
            try
            {
                if (element == null)
                    return null;
                PositioningElementInfo info = new PositioningElementInfo
                {
                    Id = element.Id.GetIntValue(),
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ?
                        Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue()) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };

                // Levels
                if (element is Level level)
                {
                    // Convert to mm
                    info.Elevation = level.Elevation * 304.8;
                }
                // Grids
                else if (element is Grid grid)
                {
                    Curve curve = grid.Curve;
                    if (curve != null)
                    {
                        XYZ start = curve.GetEndPoint(0);
                        XYZ end = curve.GetEndPoint(1);
                        // Create JZLine (converted to mm)
                        info.GridLine = new JZLine(
                            start.X * 304.8, start.Y * 304.8, start.Z * 304.8,
                            end.X * 304.8, end.Y * 304.8, end.Z * 304.8);
                    }
                }

                // Get level info
                info.Level = GetElementLevel(doc, element);

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error building datum element info: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// </summary>
        public static SpatialElementInfo CreateSpatialElementInfo(Document doc, Element element)
        {
            try
            {
                if (element == null || !(element is SpatialElement))
                    return null;
                SpatialElement spatialElement = element as SpatialElement;
                SpatialElementInfo info = new SpatialElementInfo
                {
                    Id = element.Id.GetIntValue(),
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ?
                        Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue()) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };

                // Get the room or area number
                if (element is Room room)
                {
                    info.Number = room.Number;
                    // Convert to mm³
                    info.Volume = room.Volume * Math.Pow(304.8, 3);
                }
                else if (element is Area area)
                {
                    info.Number = area.Number;
                }

                // Get area
                Parameter areaParam = element.get_Parameter(BuiltInParameter.ROOM_AREA);
                if (areaParam != null && areaParam.HasValue)
                {
                    // Convert to mm²
                    info.Area = areaParam.AsDouble() * Math.Pow(304.8, 2);
                }

                // Get perimeter
                Parameter perimeterParam = element.get_Parameter(BuiltInParameter.ROOM_PERIMETER);
                if (perimeterParam != null && perimeterParam.HasValue)
                {
                    // Convert to mm
                    info.Perimeter = perimeterParam.AsDouble() * 304.8;
                }

                // Get level
                info.Level = GetElementLevel(doc, element);

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error building spatial element info: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// </summary>
        public static ViewInfo CreateViewInfo(Document doc, Element element)
        {
            try
            {
                if (element == null || !(element is View))
                    return null;
                View view = element as View;

                ViewInfo info = new ViewInfo
                {
                    Id = element.Id.GetIntValue(),
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ?
                        Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue()) : null,
                    ElementClass = element.GetType().Name,
                    ViewType = view.ViewType.ToString(),
                    Scale = view.Scale,
                    IsTemplate = view.IsTemplate,
                    DetailLevel = view.DetailLevel.ToString(),
                    BoundingBox = GetBoundingBoxInfo(element)
                };

                // Get the level associated with the view
                if (view is ViewPlan viewPlan && viewPlan.GenLevel != null)
                {
                    Level level = viewPlan.GenLevel;
                    info.AssociatedLevel = new LevelInfo
                    {
                        Id = level.Id.GetIntValue(),
                        Name = level.Name,
                        Height = level.Elevation * 304.8 // Convert to mm
                    };
                }

                // Check whether the view is open and active
                UIDocument uidoc = new UIDocument(doc);

                // Get all open views
                IList<UIView> openViews = uidoc.GetOpenUIViews();

                foreach (UIView uiView in openViews)
                {
                    // Check whether the view is open
                    if (uiView.ViewId.GetValue() == view.Id.GetValue())
                    {
                        info.IsOpen = true;

                        // Check whether the view is the active view
                        if (uidoc.ActiveView.Id.GetValue() == view.Id.GetValue())
                        {
                            info.IsActive = true;
                        }
                        break;
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error building view element info: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// </summary>
        public static AnnotationInfo CreateAnnotationInfo(Document doc, Element element)
        {
            try
            {
                if (element == null)
                    return null;
                AnnotationInfo info = new AnnotationInfo
                {
                    Id = element.Id.GetIntValue(),
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ?
                        Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue()) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };

                // Get the owner view
                Parameter viewParam = element.get_Parameter(BuiltInParameter.VIEW_NAME);
                if (viewParam != null && viewParam.HasValue)
                {
                    info.OwnerView = viewParam.AsString();
                }
                else if (element.OwnerViewId != ElementId.InvalidElementId)
                {
                    View ownerView = doc.GetElement(element.OwnerViewId) as View;
                    info.OwnerView = ownerView?.Name;
                }

                // Text notes
                if (element is TextNote textNote)
                {
                    info.TextContent = textNote.Text;
                    XYZ position = textNote.Coord;
                    // Convert to mm
                    info.Position = new JZPoint(
                        position.X * 304.8,
                        position.Y * 304.8,
                        position.Z * 304.8);
                }
                // Dimensions
                else if (element is Dimension dimension)
                {
                    info.DimensionValue = dimension.Value.ToString();
                    XYZ origin = dimension.Origin;
                    // Convert to mm
                    info.Position = new JZPoint(
                        origin.X * 304.8,
                        origin.Y * 304.8,
                        origin.Z * 304.8);
                }
                // Other annotation elements
                else if (element is AnnotationSymbol annotationSymbol)
                {
                    if (annotationSymbol.Location is LocationPoint locationPoint)
                    {
                        XYZ position = locationPoint.Point;
                        // Convert to mm
                        info.Position = new JZPoint(
                            position.X * 304.8,
                            position.Y * 304.8,
                            position.Z * 304.8);
                    }
                }
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error building annotation element info: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// </summary>
        public static GroupOrLinkInfo CreateGroupOrLinkInfo(Document doc, Element element)
        {
            try
            {
                if (element == null)
                    return null;
                GroupOrLinkInfo info = new GroupOrLinkInfo
                {
                    Id = element.Id.GetIntValue(),
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ?
                        Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue()) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };

                // Groups
                if (element is Group group)
                {
                    ICollection<ElementId> memberIds = group.GetMemberIds();
                    info.MemberCount = memberIds?.Count;
                    info.GroupType = group.GroupType?.Name;
                }
                // Links
                else if (element is RevitLinkInstance linkInstance)
                {
                    RevitLinkType linkType = doc.GetElement(linkInstance.GetTypeId()) as RevitLinkType;
                    if (linkType != null)
                    {
                        ExternalFileReference extFileRef = linkType.GetExternalFileReference();
                        // Get the absolute path
                        string absPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(extFileRef.GetAbsolutePath());
                        info.LinkPath = absPath;

                        // Get the link status via GetLinkedFileStatus
                        LinkedFileStatus linkStatus = linkType.GetLinkedFileStatus();
                        info.LinkStatus = linkStatus.ToString();
                    }
                    else
                    {
                        info.LinkStatus = LinkedFileStatus.Invalid.ToString();
                    }

                    // Get the location
                    LocationPoint location = linkInstance.Location as LocationPoint;
                    if (location != null)
                    {
                        XYZ point = location.Point;
                        // Convert to mm
                        info.Position = new JZPoint(
                            point.X * 304.8,
                            point.Y * 304.8,
                            point.Z * 304.8);
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error building group/link info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// </summary>
        public static ElementBasicInfo CreateElementBasicInfo(Document doc, Element element)
        {
            try
            {
                if (element == null)
                    return null;
                ElementBasicInfo basicInfo = new ElementBasicInfo
                {
                    Id = element.Id.GetIntValue(),
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element?.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ?
                        Enum.GetName(typeof(BuiltInCategory), element.Category.Id.GetIntValue()) : null,
                    BoundingBox = GetBoundingBoxInfo(element)
                };
                return basicInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error building basic element info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="element">System family element (wall, floor, door, etc.)</param>
        /// <returns>Parameter info object, or null if invalid</returns>
        public static ParameterInfo GetThicknessInfo(Element element)
        {
            if (element == null)
            {
                return null;
            }

            // Get the element type
            ElementType elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (elementType == null)
            {
                return null;
            }

            // Get the built-in thickness parameter for each element type
            Parameter thicknessParam = null;

            if (elementType is WallType)
            {
                thicknessParam = elementType.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);
            }
            else if (elementType is FloorType)
            {
                thicknessParam = elementType.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
            }
            else if (elementType is FamilySymbol familySymbol)
            {
                switch (familySymbol.Category?.Id.GetIntValue())
                {
                    case (int)BuiltInCategory.OST_Doors:
                    case (int)BuiltInCategory.OST_Windows:
                        thicknessParam = elementType.get_Parameter(BuiltInParameter.FAMILY_THICKNESS_PARAM);
                        break;
                }
            }
            else if (elementType is CeilingType)
            {
                thicknessParam = elementType.get_Parameter(BuiltInParameter.CEILING_THICKNESS);
            }

            if (thicknessParam != null && thicknessParam.HasValue)
            {
                return new ParameterInfo
                {
                    Name = "Thickness",
                    Value = $"{thicknessParam.AsDouble() * 304.8}"
                };
            }
            return null;
        }

        /// <summary>
        /// </summary>
        public static LevelInfo GetElementLevel(Document doc, Element element)
        {
            try
            {
                Level level = null;

                // Level lookup differs per element type
                if (element is Wall wall) // Wall
                {
                    level = doc.GetElement(wall.LevelId) as Level;
                }
                else if (element is Floor floor) // Floor
                {
                    Parameter levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                    if (levelParam != null && levelParam.HasValue)
                    {
                        level = doc.GetElement(levelParam.AsElementId()) as Level;
                    }
                }
                else if (element is FamilyInstance familyInstance) // Family instance (incl. generic models)
                {
                    // Try the family instance's level parameter
                    Parameter levelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                    if (levelParam != null && levelParam.HasValue)
                    {
                        level = doc.GetElement(levelParam.AsElementId()) as Level;
                    }
                    // Fall back to SCHEDULE_LEVEL_PARAM
                    if (level == null)
                    {
                        levelParam = familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
                        if (levelParam != null && levelParam.HasValue)
                        {
                            level = doc.GetElement(levelParam.AsElementId()) as Level;
                        }
                    }
                }
                else // Other elements
                {
                    // Try the generic level parameter
                    Parameter levelParam = element.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
                    if (levelParam != null && levelParam.HasValue)
                    {
                        level = doc.GetElement(levelParam.AsElementId()) as Level;
                    }
                }

                if (level != null)
                {
                    LevelInfo levelInfo = new LevelInfo
                    {
                        Id = level.Id.GetIntValue(),
                        Name = level.Name,
                        Height = level.Elevation * 304.8
                    };
                    return levelInfo;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// </summary>
        public static BoundingBoxInfo GetBoundingBoxInfo(Element element)
        {
            try
            {
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox == null)
                    return null;
                return new BoundingBoxInfo
                {
                    Min = new JZPoint(
                        bbox.Min.X * 304.8,
                        bbox.Min.Y * 304.8,
                        bbox.Min.Z * 304.8),
                    Max = new JZPoint(
                        bbox.Max.X * 304.8,
                        bbox.Max.Y * 304.8,
                        bbox.Max.Z * 304.8)
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="boundingBoxInfo">Bounding box info</param>
        /// <returns>Parameter info object, or null if invalid</returns>
        public static ParameterInfo GetBoundingBoxHeight(BoundingBoxInfo boundingBoxInfo)
        {
            try
            {
                // Parameter validation
                if (boundingBoxInfo?.Min == null || boundingBoxInfo?.Max == null)
                {
                    return null;
                }

                // Height is the Z-axis extent
                double height = Math.Abs(boundingBoxInfo.Max.Z - boundingBoxInfo.Min.Z);

                return new ParameterInfo
                {
                    Name = "Height",
                    Value = $"{height}"
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="element">Revit element</param>
        /// <returns>List of parameter info</returns>
        public static List<ParameterInfo> GetDimensionParameters(Element element)
        {
            // Null check
            if (element == null)
            {
                return new List<ParameterInfo>();
            }

            var parameters = new List<ParameterInfo>();

            // Get all parameters of the element
            foreach (Parameter param in element.Parameters)
            {
                try
                {
                    // Skip invalid parameters
                    if (!param.HasValue || param.IsReadOnly)
                    {
                        continue;
                    }

                    // If the parameter is dimension-related
                    if (IsDimensionParameter(param))
                    {
                        // Get the parameter value as a string
                        string value = param.AsValueString();

                        // Add non-empty values to the list
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            parameters.Add(new ParameterInfo
                            {
                                Name = param.Definition.Name,
                                Value = value
                            });
                        }
                    }
                }
                catch
                {
                    // On error, continue with the next parameter
                    continue;
                }
            }

            // Return sorted by parameter name
            return parameters.OrderBy(p => p.Name).ToList();
        }

        /// <summary>
        /// </summary>
        public static bool IsDimensionParameter(Parameter param)
        {

#if REVIT2023_OR_GREATER
            // Revit 2023+: use Definition.GetDataType() to get the parameter type
            ForgeTypeId paramTypeId = param.Definition.GetDataType();

            // Check whether the parameter is dimension-related
            bool isDimensionType = paramTypeId.Equals(SpecTypeId.Length) ||
                                   paramTypeId.Equals(SpecTypeId.Angle) ||
                                   paramTypeId.Equals(SpecTypeId.Area) ||
                                   paramTypeId.Equals(SpecTypeId.Volume);
            // Store dimension parameters only
            return isDimensionType;
#else
            // Check whether the parameter is dimension-related
            bool isDimensionType = param.Definition.ParameterType == ParameterType.Length ||
                                   param.Definition.ParameterType == ParameterType.Angle ||
                                   param.Definition.ParameterType == ParameterType.Area ||
                                   param.Definition.ParameterType == ParameterType.Volume;

            // Store dimension parameters only
            return isDimensionType;
#endif
        }

    }

    /// <summary>
    /// </summary>
    public class ElementInstanceInfo
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Id
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public int TypeId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public int RoomId { get; set; }
        /// <summary>
        /// </summary>
        public LevelInfo Level { get; set; }
        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }
        /// <summary>
        /// </summary>
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();

    }

    /// <summary>
    /// </summary>
    public class ElementTypeInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Id
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();

    }

    /// <summary>
    /// </summary>
    public class PositioningElementInfo
    {
        /// <summary>
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public string ElementClass { get; set; }
        /// <summary>
        /// </summary>
        public double? Elevation { get; set; }
        /// <summary>
        /// </summary>
        public LevelInfo Level { get; set; }
        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }
        /// <summary>
        /// </summary>
        public JZLine GridLine { get; set; }
    }
    /// <summary>
    /// </summary>
    public class SpatialElementInfo
    {
        /// <summary>
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public string ElementClass { get; set; }
        /// <summary>
        /// </summary>
        public double? Area { get; set; }
        /// <summary>
        /// </summary>
        public double? Volume { get; set; }
        /// <summary>
        /// </summary>
        public double? Perimeter { get; set; }
        /// <summary>
        /// </summary>
        public LevelInfo Level { get; set; }

        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }
    }
    /// <summary>
    /// </summary>
    public class ViewInfo
    {
        /// <summary>
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public string ElementClass { get; set; }

        /// <summary>
        /// </summary>
        public string ViewType { get; set; }

        /// <summary>
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// </summary>
        public bool IsTemplate { get; set; }

        /// <summary>
        /// </summary>
        public string DetailLevel { get; set; }

        /// <summary>
        /// </summary>
        public LevelInfo AssociatedLevel { get; set; }

        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }

        /// <summary>
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        /// </summary>
        public bool IsActive { get; set; }
    }
    /// <summary>
    /// </summary>
    public class AnnotationInfo
    {
        /// <summary>
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public string ElementClass { get; set; }
        /// <summary>
        /// </summary>
        public string OwnerView { get; set; }
        /// <summary>
        /// </summary>
        public string TextContent { get; set; }
        /// <summary>
        /// </summary>
        public JZPoint Position { get; set; }

        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }
        /// <summary>
        /// </summary>
        public string DimensionValue { get; set; }
    }
    /// <summary>
    /// </summary>
    public class GroupOrLinkInfo
    {
        /// <summary>
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }
        /// <summary>
        /// </summary>
        public string ElementClass { get; set; }
        /// <summary>
        /// </summary>
        public int? MemberCount { get; set; }
        /// <summary>
        /// </summary>
        public string GroupType { get; set; }
        /// <summary>
        /// </summary>
        public string LinkStatus { get; set; }
        /// <summary>
        /// </summary>
        public string LinkPath { get; set; }
        /// <summary>
        /// </summary>
        public JZPoint Position { get; set; }

        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }
    }
    /// <summary>
    /// </summary>
    public class ElementBasicInfo
    {
        /// <summary>
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// </summary>
        public string UniqueId { get; set; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string FamilyName { get; set; }
        /// <summary>
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// </summary>
        public string BuiltInCategory { get; set; }

        /// <summary>
        /// </summary>
        public BoundingBoxInfo BoundingBox { get; set; }
    }



    /// <summary>
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// </summary>
    public class BoundingBoxInfo
    {
        public JZPoint Min { get; set; }
        public JZPoint Max { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LevelInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Height { get; set; }
    }



}
