using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgModifyNodeCommand : ExternalEventCommandBase
    {
        private KgModifyNodeEventHandler _handler => (KgModifyNodeEventHandler)Handler;

        public override string CommandName => "kg_modify_node";

        public KgModifyNodeCommand(UIApplication uiApp)
            : base(new KgModifyNodeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var llmIdTok = parameters?["llm_id"];
                if (llmIdTok == null || llmIdTok.Type == JTokenType.Null)
                    throw new ArgumentException("kg_modify_node requires llm_id");
                string llmId = llmIdTok.ToString();

                var updatesTok = parameters?["updates"];
                if (!(updatesTok is JObject jo))
                    throw new ArgumentException("kg_modify_node requires an updates JSON object");
                var updates = jo.ToObject<Dictionary<string, object>>();

                _handler.SetParameters(llmId, updates);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_modify_node timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_modify_node failed: {ex.Message}");
            }
        }
    }
}
