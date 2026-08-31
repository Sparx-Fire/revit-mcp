using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
    public class CreateMEPCurveEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string MEPType { get; private set; }
        public double StartX { get; private set; }
        public double StartY { get; private set; }
        public double StartZ { get; private set; }
        public double EndX { get; private set; }
        public double EndY { get; private set; }
        public double EndZ { get; private set; }
        public double Level { get; private set; }
        public double Diameter { get; private set; }
        public string SystemType { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        public void SetParameters(string mepType, double startX, double startY, double startZ, double endX, double endY, double endZ, double level, double diameter, string systemType)
        {
            MEPType = mepType;
            StartX = startX;
            StartY = startY;
            StartZ = startZ;
            EndX = endX;
            EndY = endY;
            EndZ = endZ;
            Level = level;
            Diameter = diameter;
            SystemType = systemType;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create MEP Curve"))
                {
                    trans.Start();

                    XYZ startPt = new XYZ(StartX / 304.8, StartY / 304.8, Level / 304.8);
                    XYZ endPt = new XYZ(EndX / 304.8, EndY / 304.8, Level / 304.8);
                    double diameterFt = Diameter / 304.8;

                    List<int> elementIds = new List<int>();

                    switch (MEPType.ToLowerInvariant())
                    {
                        case "duct":
                        {
                            MechanicalSystemType ductSystemType = null;
                            if (!string.IsNullOrEmpty(SystemType))
                            {
                                ductSystemType = new FilteredElementCollector(doc)
                                    .OfClass(typeof(MechanicalSystemType))
                                    .Cast<MechanicalSystemType>()
                                    .FirstOrDefault(dst => dst.Name.Contains(SystemType));
                            }

                            Duct duct = VersionCompat.CreateDuct(doc, ductSystemType?.Id ?? ElementId.InvalidElementId, startPt, endPt, ElementId.InvalidElementId);
                            if (duct != null)
                            {
                                duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM)?.Set(diameterFt);
                                elementIds.Add(duct.Id.GetIntValue());
                            }
                            break;
                        }
                        case "pipe":
                        {
                            Pipe pipe = VersionCompat.CreatePipe(doc, ElementId.InvalidElementId, startPt, endPt, ElementId.InvalidElementId);
                            if (pipe != null)
                            {
                                pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(diameterFt);
                                elementIds.Add(pipe.Id.GetIntValue());
                            }
                            break;
                        }
                        case "conduit":
                        {
                            var conduit = VersionCompat.CreateConduit(doc, ElementId.InvalidElementId, startPt, endPt, ElementId.InvalidElementId) as MEPCurve;
                            if (conduit != null)
                            {
                                conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM)?.Set(diameterFt);
                                elementIds.Add(conduit.Id.GetIntValue());
                            }
                            break;
                        }
                        default:
                            Result = new AIResult<List<int>> { Success = false, Message = $"Unknown MEP type: {MEPType}. Use 'duct', 'pipe', or 'conduit'" };
                            return;
                    }

                    trans.Commit();

                    Result = new AIResult<List<int>>
                    {
                        Success = true,
                        Message = $"{MEPType} curve created successfully",
                        Response = elementIds
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error creating MEP curve: {ex.Message}"
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

        public string GetName() => "Create MEP Curve";
    }
}
