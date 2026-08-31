using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Query;

namespace RevitMCPCommandSet.Commands.Query
{
    public class QueryViewRangeCommand : ExternalEventCommandBase
    {
        private QueryViewRangeEventHandler _handler => (QueryViewRangeEventHandler)Handler;
        public override string CommandName => "query_view_range";
        public QueryViewRangeCommand(UIApplication uiApp)
            : base(new QueryViewRangeEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"].Value<int>();
                _handler.SetParameters(viewId);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Query view range timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to query view range: {ex.Message}");
            }
        }
    }
}
