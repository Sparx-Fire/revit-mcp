using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitMCPCommandSet.Commands
{
    public class OperateElementCommand : ExternalEventCommandBase
    {
        private OperateElementEventHandler _handler => (OperateElementEventHandler)Handler;

        /// <summary>
        /// </summary>
        public override string CommandName => "operate_element";

        /// <summary>
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        public OperateElementCommand(UIApplication uiApp)
            : base(new OperateElementEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                OperationSetting data = new OperationSetting();
                // Parse parameters
                data = parameters["data"].ToObject<OperationSetting>();
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
                    throw new TimeoutException("Operate element timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to operate on element: {ex.Message}");
            }
        }
    }
}
