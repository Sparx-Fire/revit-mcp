using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands
{
    public class CreateLineElementCommand : ExternalEventCommandBase
    {
        private CreateLineElementEventHandler _handler => (CreateLineElementEventHandler)Handler;

        /// <summary>
        /// </summary>
        public override string CommandName => "create_line_based_element";

        /// <summary>
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        public CreateLineElementCommand(UIApplication uiApp)
            : base(new CreateLineElementEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<LineElement> data = new List<LineElement>();
                // Parse parameters
                data = parameters["data"].ToObject<List<LineElement>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Input data from AI is null");

                // Set line-based element parameters
                _handler.SetParameters(data);

                // Raise the external event and wait for completion
                if (RaiseAndWaitForCompletion(10000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create line-based element operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create line-based element: {ex.Message}");
            }
        }
    }
}
