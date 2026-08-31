using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Query;

namespace RevitMCPCommandSet.Commands.Query
{
    public class QueryParametersCommand : ExternalEventCommandBase
    {
        private QueryParametersEventHandler _handler => (QueryParametersEventHandler)Handler;
        public override string CommandName => "query_parameters";
        public QueryParametersCommand(UIApplication uiApp)
            : base(new QueryParametersEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int elementId = parameters["elementId"].Value<int>();
                _handler.SetParameters(elementId);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Query parameters timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to query parameters: {ex.Message}");
            }
        }
    }
}
