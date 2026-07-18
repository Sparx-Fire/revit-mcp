using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    public class CreateSurfaceElementCommand : ExternalEventCommandBase
    {
        private CreateSurfaceElementEventHandler _handler => (CreateSurfaceElementEventHandler)Handler;

        /// <summary>
        /// </summary>
        public override string CommandName => "create_surface_based_element";

        /// <summary>
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        public CreateSurfaceElementCommand(UIApplication uiApp)
            : base(new CreateSurfaceElementEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<SurfaceElement> data = new List<SurfaceElement>();
                // Parse parameters
                data = parameters["data"].ToObject<List<SurfaceElement>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Input data from AI is null");

                // Set surface-based element parameters
                _handler.SetParameters(data);

                // Raise the external event and wait for completion
                if (RaiseAndWaitForCompletion(10000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create surface-based element operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create surface-based element: {ex.Message}");
            }
        }
    }
}
