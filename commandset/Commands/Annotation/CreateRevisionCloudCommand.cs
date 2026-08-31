using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Annotation;

namespace RevitMCPCommandSet.Commands.Annotation
{
    public class CreateRevisionCloudCommand : ExternalEventCommandBase
    {
        private CreateRevisionCloudEventHandler _handler => (CreateRevisionCloudEventHandler)Handler;

        public override string CommandName => "create_revision_cloud";

        public CreateRevisionCloudCommand(UIApplication uiApp)
            : base(new CreateRevisionCloudEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int revisionId = parameters["revisionId"]?.Value<int>() ?? 0;
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                JArray pointsArray = parameters["points"] as JArray;

                List<JObject> points = new List<JObject>();
                if (pointsArray != null)
                {
                    foreach (var item in pointsArray)
                    {
                        points.Add(item as JObject);
                    }
                }

                _handler.SetParameters(revisionId, viewId, points);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create revision cloud operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create revision cloud: {ex.Message}");
            }
        }
    }
}
