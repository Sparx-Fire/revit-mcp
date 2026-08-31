using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateFilledRegionCommand : ExternalEventCommandBase
    {
        private CreateFilledRegionEventHandler _handler => (CreateFilledRegionEventHandler)Handler;

        public override string CommandName => "create_filled_region";

        public CreateFilledRegionCommand(UIApplication uiApp)
            : base(new CreateFilledRegionEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                string filledRegionTypeName = parameters["filledRegionTypeName"]?.Value<string>();
                JArray boundaryArray = parameters["boundary"] as JArray;

                List<List<JObject>> boundary = new List<List<JObject>>();
                if (boundaryArray != null)
                {
                    foreach (var loop in boundaryArray)
                    {
                        JArray loopArray = loop as JArray;
                        if (loopArray != null)
                        {
                            List<JObject> points = new List<JObject>();
                            foreach (var pt in loopArray)
                            {
                                points.Add(pt as JObject);
                            }
                            boundary.Add(points);
                        }
                    }
                }

                _handler.SetParameters(viewId, boundary, filledRegionTypeName);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create filled region operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create filled region: {ex.Message}");
            }
        }
    }
}
