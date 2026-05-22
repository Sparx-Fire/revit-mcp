using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands
{
    public class BatchSetParametersCommand : ExternalEventCommandBase
    {
        private BatchSetParametersEventHandler _handler => (BatchSetParametersEventHandler)Handler;

        public override string CommandName => "batch_set_parameters";

        public BatchSetParametersCommand(UIApplication uiApp)
            : base(new BatchSetParametersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                BatchSetParametersSetting data;
                if (parameters?["data"] != null)
                    data = parameters["data"].ToObject<BatchSetParametersSetting>();
                else
                    data = parameters.ToObject<BatchSetParametersSetting>();

                if (data == null)
                    throw new ArgumentNullException(nameof(data), "missing 'data' parameter");

                _handler.SetParameters(data);

                // Batch can take a while ; allow up to 60s for very large
                // batches (1000+ ops). Typical 30-op batch finishes in ~1-2s.
                if (!RaiseAndWaitForCompletion(60000))
                    throw new TimeoutException("batch_set_parameters timed out");

                return _handler.Result;
            }
            catch (Exception ex)
            {
                throw new Exception($"batch_set_parameters failed: {ex.Message}");
            }
        }
    }
}
