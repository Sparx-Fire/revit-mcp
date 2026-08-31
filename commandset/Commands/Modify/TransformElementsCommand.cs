using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Modify;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class TransformElementsCommand : ExternalEventCommandBase
    {
        private TransformElementsEventHandler _handler => (TransformElementsEventHandler)Handler;
        public override string CommandName => "transform_elements";
        public TransformElementsCommand(UIApplication uiApp)
            : base(new TransformElementsEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters["elementIds"].ToObject<int[]>();
                string transformType = parameters["transformType"].Value<string>();
                var transformParams = parameters["params"] as JObject;
                _handler.SetParameters(elementIds, transformType, transformParams);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Transform elements timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to transform elements: {ex.Message}");
            }
        }
    }
}
