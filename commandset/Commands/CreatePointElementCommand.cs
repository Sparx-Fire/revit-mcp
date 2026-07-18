using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    public class CreatePointElementCommand :    ExternalEventCommandBase
    {
        private CreatePointElementEventHandler _handler => (CreatePointElementEventHandler)Handler;

        /// <summary>
        /// </summary>
        public override string CommandName => "create_point_based_element";

        /// <summary>
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        public CreatePointElementCommand(UIApplication uiApp)
            : base(new CreatePointElementEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<PointElement> data = new List<PointElement>();
                // Parse parameters
                data = parameters["data"].ToObject<List<PointElement>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Input data from AI is null");

                // Set point-based element parameters
                _handler.SetParameters(data);

                // Raise the external event and wait for completion
                if (RaiseAndWaitForCompletion(10000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create point-based element operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create point-based element: {ex.Message}");
            }
        }
    }

}
