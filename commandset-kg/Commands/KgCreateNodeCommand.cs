using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgCreateNodeCommand : ExternalEventCommandBase
    {
        private KgCreateNodeEventHandler _handler => (KgCreateNodeEventHandler)Handler;

        public override string CommandName => "kg_create_node";

        public KgCreateNodeCommand(UIApplication uiApp)
            : base(new KgCreateNodeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var nodeTypeTok = parameters?["node_type"];
                if (nodeTypeTok == null || nodeTypeTok.Type == JTokenType.Null)
                    throw new ArgumentException("kg_create_node requires node_type");
                string nodeType = nodeTypeTok.ToString();

                var attrsTok = parameters?["attrs"];
                Dictionary<string, object> attrs = null;
                if (attrsTok is JObject jo)
                    attrs = jo.ToObject<Dictionary<string, object>>();
                else if (attrsTok != null && attrsTok.Type != JTokenType.Null)
                    throw new ArgumentException("kg_create_node attrs must be a JSON object");

                _handler.SetParameters(nodeType, attrs);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_create_node timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_create_node failed: {ex.Message}");
            }
        }
    }
}
