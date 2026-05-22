using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgDeleteNodeCommand : ExternalEventCommandBase
    {
        private KgDeleteNodeEventHandler _handler => (KgDeleteNodeEventHandler)Handler;

        public override string CommandName => "kg_delete_node";

        public KgDeleteNodeCommand(UIApplication uiApp)
            : base(new KgDeleteNodeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var llmIdTok = parameters?["llm_id"];
                if (llmIdTok == null || llmIdTok.Type == JTokenType.Null)
                    throw new ArgumentException("kg_delete_node requires llm_id");
                string llmId = llmIdTok.ToString();

                _handler.SetParameters(llmId);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_delete_node timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_delete_node failed: {ex.Message}");
            }
        }
    }
}
