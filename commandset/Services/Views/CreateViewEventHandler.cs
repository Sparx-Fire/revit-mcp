using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ViewCreationInfo> CreatedInfo { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<ViewCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var viewIds = new List<int>();
                _warnings.Clear();

                foreach (var info in CreatedInfo)
                {
                    View view = null;
                    string viewTypeLower = info.ViewType?.ToLowerInvariant() ?? "";

                    using (Transaction trans = new Transaction(doc, "Create View"))
                    {
                        trans.Start();

                        if (viewTypeLower == "3d" || viewTypeLower == "三维")
                        {
                            view = Create3DView(info);
                        }
                        else if (viewTypeLower == "floorplan" || viewTypeLower == "floor plan")
                        {
                            view = CreatePlanView(info, ViewFamily.FloorPlan);
                        }
                        else if (viewTypeLower == "ceilingplan" || viewTypeLower == "ceiling plan")
                        {
                            view = CreatePlanView(info, ViewFamily.CeilingPlan);
                        }
                        else if (viewTypeLower == "elevation")
                        {
                            view = CreateElevationView(info);
                        }
                        else if (viewTypeLower == "section")
                        {
                            view = CreateSectionView(info);
                        }
                        else
                        {
                            view = CreatePlanView(info, ViewFamily.FloorPlan);
                        }

                        if (view != null)
                        {
                            if (!string.IsNullOrEmpty(info.Name))
                            {
                                view.Name = info.Name;
                            }

                            if (info.Scale > 0 && view.CanViewBeDuplicated(ViewDuplicateOption.Duplicate))
                            {
                                view.Scale = info.Scale;
                            }

                            if (!string.IsNullOrEmpty(info.DetailLevel))
                            {
                                switch (info.DetailLevel.ToLowerInvariant())
                                {
                                    case "coarse":
                                        view.DetailLevel = ViewDetailLevel.Coarse;
                                        break;
                                    case "medium":
                                        view.DetailLevel = ViewDetailLevel.Medium;
                                        break;
                                    case "fine":
                                        view.DetailLevel = ViewDetailLevel.Fine;
                                        break;
                                }
                            }

                            if (!string.IsNullOrEmpty(info.TemplateId) && int.TryParse(info.TemplateId, out int templateIntId))
                            {
                                ElementId templateId = new ElementId(templateIntId);
                                View templateView = doc.GetElement(templateId) as View;
                                if (templateView != null && templateView.IsTemplate)
                                {
                                    view.ViewTemplateId = templateId;
                                }
                            }

                            foreach (var param in info.Parameters)
                            {
                                Parameter viewParam = view.LookupParameter(param.Key);
                                if (viewParam != null)
                                {
                                    SetParameterValue(viewParam, param.Value);
                                }
                            }

                            viewIds.Add(view.Id.GetIntValue());
                        }

                        trans.Commit();
                    }
                }

                string message = $"Successfully created {viewIds.Count} view(s).";
                if (_warnings.Count > 0)
                {
                    message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = viewIds,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error creating view: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private View Create3DView(ViewCreationInfo info)
        {
            View3D view3D = null;
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.ThreeDimensional);

            if (vft != null)
            {
                view3D = View3D.CreateIsometric(doc, vft.Id);
            }

            return view3D;
        }

        private View CreatePlanView(ViewCreationInfo info, ViewFamily viewFamily)
        {
            Level level = FindLevel(info.LevelElevation);
            if (level == null)
            {
                _warnings.Add($"No level found near elevation {info.LevelElevation}mm. Using first available level.");
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault();
            }

            if (level == null)
            {
                _warnings.Add("No level available in project.");
                return null;
            }

            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vftype => vftype.ViewFamily == viewFamily);

            if (vft == null)
            {
                _warnings.Add($"No view family type found for {viewFamily}.");
                return null;
            }

            ViewPlan viewPlan = null;

            if (viewFamily == ViewFamily.FloorPlan)
            {
                viewPlan = ViewPlan.Create(doc, vft.Id, level.Id);
            }
            else if (viewFamily == ViewFamily.CeilingPlan)
            {
                viewPlan = ViewPlan.Create(doc, vft.Id, level.Id);
            }

            return viewPlan;
        }

        private View CreateElevationView(ViewCreationInfo info)
        {
            Level level = FindLevel(info.LevelElevation);
            if (level == null)
            {
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault();
            }

            if (level == null) return null;

            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Elevation);

            if (vft == null) return null;

#if REVIT2026_OR_GREATER
            // R26: CreateElevationMarker(Document, ElementId, XYZ, int)
            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, vft.Id, new XYZ(0, 0, level.Elevation), 100);
#elif REVIT2025_OR_GREATER
            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, vft.Id, level.Id, new XYZ(0, 0, level.Elevation));
#else
            // R20-R21: Elevation creation not supported via this API
            return null;
#endif

#if REVIT2025_OR_GREATER
            if (marker != null)
                {
                    ViewSection elevationView = VersionCompat.CreateElevationView(marker, level.Id, 0);

                    if (elevationView == null)
                    {
                        return null;
                    }

                if (info.Direction != null)
                {
                    try
                    {
                        XYZ direction = new XYZ(
                            (double)(info.Direction.GetType().GetProperty("x")?.GetValue(info.Direction) ?? 0),
                            (double)(info.Direction.GetType().GetProperty("y")?.GetValue(info.Direction) ?? 1),
                            (double)(info.Direction.GetType().GetProperty("z")?.GetValue(info.Direction) ?? 0)
                        );

                        if (direction.GetLength() > 0)
                        {
                            direction = direction.Normalize();
                        }
                    }
                    catch { }
                }

                return elevationView;
            }
#endif

            return null;
        }

        private View CreateSectionView(ViewCreationInfo info)
        {
            Level level = FindLevel(info.LevelElevation);
            if (level == null)
            {
                level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault();
            }

            if (level == null) return null;

            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Section);

            if (vft == null) return null;

            XYZ origin = new XYZ(0, 0, level.Elevation);
            XYZ direction = new XYZ(0, 1, 0);
            if (info.Direction != null)
            {
                try
                {
                    double dx = (double)(info.Direction.GetType().GetProperty("x")?.GetValue(info.Direction) ?? 0);
                    double dy = (double)(info.Direction.GetType().GetProperty("y")?.GetValue(info.Direction) ?? 1);
                    double dz = (double)(info.Direction.GetType().GetProperty("z")?.GetValue(info.Direction) ?? 0);
                    XYZ dir = new XYZ(dx, dy, dz);
                    if (dir.GetLength() > 1e-9)
                        direction = dir.Normalize();
                }
                catch { }
            }

            XYZ boundingBoxMin = new XYZ(-50, -50, -50);
            XYZ boundingBoxMax = new XYZ(50, 50, 50);
            BoundingBoxXYZ sectionBox = new BoundingBoxXYZ
            {
                Min = boundingBoxMin,
                Max = boundingBoxMax,
                Transform = Transform.CreateTranslation(origin)
            };

            ViewSection sectionView = ViewSection.CreateSection(doc, vft.Id, sectionBox);

            return sectionView;
        }

        private Level FindLevel(double elevationMm)
        {
            if (elevationMm <= 0) return null;

            double elevationFt = elevationMm / 304.8;

            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => Math.Abs(l.Elevation - elevationFt))
                .FirstOrDefault();
        }

        private void SetParameterValue(Parameter param, object value)
        {
            if (value == null) return;

            switch (param.StorageType)
            {
                case StorageType.Integer:
                    if (value is long l) param.Set((int)l);
                    else if (value is int i) param.Set(i);
                    else if (value is bool b) param.Set(b ? 1 : 0);
                    break;
                case StorageType.Double:
                    if (value is double d) param.Set(d);
                    else if (value is long ld) param.Set((double)ld);
                    break;
                case StorageType.String:
                    param.Set(value.ToString());
                    break;
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Create View";
    }
}
