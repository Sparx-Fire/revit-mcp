using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Query;

namespace RevitMCPCommandSet.Commands.Query
{
    public class QueryReferencesCommand : ExternalEventCommandBase
    {
        private QueryReferencesEventHandler _handler => (QueryReferencesEventHandler)Handler;
        public override string CommandName => "query_references";
        public QueryReferencesCommand(UIApplication uiApp)
            : base(new QueryReferencesEventHandler(), uiApp)
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
                throw new TimeoutException("Query references timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to query references: {ex.Message}");
            }
        }
    }
}
