using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class TransformElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int[] ElementIds { get; private set; }
        public string TransformType { get; private set; }
        public JObject TransformParams { get; private set; }
        public AIResult<List<int>> Result { get; private set; }

        public void SetParameters(int[] elementIds, string transformType, JObject transformParams)
        {
            ElementIds = elementIds;
            TransformType = transformType;
            TransformParams = transformParams;
            _resetEvent.Reset(); 
        }

        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                var ids = ElementIds.Select(id => new ElementId(id)).ToList();
                var newIds = new List<int>();

                using (var trans = new Transaction(Doc, $"Transform Elements - {TransformType}"))
                {
                    trans.Start();
                    switch (TransformType.ToLower())
                    {
                        case "move":
                        {
                            double dx = TransformParams["dx"]?.Value<double>() ?? 0;
                            double dy = TransformParams["dy"]?.Value<double>() ?? 0;
                            double dz = TransformParams["dz"]?.Value<double>() ?? 0;
                            ElementTransformUtils.MoveElements(Doc, ids, new XYZ(dx, dy, dz));
                            break;
                        }
                        case "copy":
                        {
                            double dx = TransformParams["dx"]?.Value<double>() ?? 0;
                            double dy = TransformParams["dy"]?.Value<double>() ?? 0;
                            double dz = TransformParams["dz"]?.Value<double>() ?? 0;
                            var copied = ElementTransformUtils.CopyElements(Doc, ids, new XYZ(dx, dy, dz));
                            newIds = copied.Select(id => id.GetIntValue()).ToList();
                            break;
                        }
                        case "rotate":
                        {
                            var axis = TransformParams["axis"] != null ? new XYZ(
                                TransformParams["axis"]["x"]?.Value<double>() ?? 0,
                                TransformParams["axis"]["y"]?.Value<double>() ?? 0,
                                TransformParams["axis"]["z"]?.Value<double>() ?? 1
                            ) : XYZ.BasisZ;
                            var origin = TransformParams["origin"] != null ? new XYZ(
                                TransformParams["origin"]["x"]?.Value<double>() ?? 0,
                                TransformParams["origin"]["y"]?.Value<double>() ?? 0,
                                TransformParams["origin"]["z"]?.Value<double>() ?? 0
                            ) : XYZ.Zero;
                            double angle = TransformParams["angle"]?.Value<double>() ?? 0;
                            var line = Line.CreateUnbound(origin, axis);
                            ElementTransformUtils.RotateElements(Doc, ids, line, angle);
                            break;
                        }
                        case "mirror":
                        {
                            var mirrorOrigin = TransformParams["origin"] != null ? new XYZ(
                                TransformParams["origin"]["x"]?.Value<double>() ?? 0,
                                TransformParams["origin"]["y"]?.Value<double>() ?? 0,
                                TransformParams["origin"]["z"]?.Value<double>() ?? 0
                            ) : XYZ.Zero;
                            var mirrorNormal = TransformParams["normal"] != null ? new XYZ(
                                TransformParams["normal"]["x"]?.Value<double>() ?? 0,
                                TransformParams["normal"]["y"]?.Value<double>() ?? 1,
                                TransformParams["normal"]["z"]?.Value<double>() ?? 0
                            ) : XYZ.BasisY;
                            var plane = Plane.CreateByNormalAndOrigin(mirrorNormal, mirrorOrigin);
                            ElementTransformUtils.MirrorElements(Doc, ids, plane, false);
                            break;
                        }
                        default:
                            throw new ArgumentException($"Unsupported transform type: {TransformType}");
                    }
                    trans.Commit();
                }

                Result = new AIResult<List<int>> { Success = true, Response = newIds };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Transform Elements";
    }
}
