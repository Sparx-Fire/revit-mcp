using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgResolveDriftCommand : ExternalEventCommandBase
    {
        private const string RequiredConfirmString = "align-to-revit";

        private KgResolveDriftEventHandler _handler => (KgResolveDriftEventHandler)Handler;

        public override string CommandName => "kg_resolve_drift";

        public KgResolveDriftCommand(UIApplication uiApp)
            : base(new KgResolveDriftEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string nodeTypeFilter = parameters?["node_type"]?.ToString();
                if (string.IsNullOrWhiteSpace(nodeTypeFilter)) nodeTypeFilter = null;

                bool dryRun = parameters?["dry_run"]?.ToObject<bool?>() ?? false;
                string confirm = parameters?["confirm"]?.ToString();

                // Safety: refuse non-dry-run unless the caller explicitly
                // acknowledges the destructive nature of the op (soft-deletes
                // orphans, overwrites attrs, projects unknown elements).
                if (!dryRun && confirm != RequiredConfirmString)
                {
                    throw new ArgumentException(
                        "kg_resolve_drift: non-dry-run requires confirm=\""
                        + RequiredConfirmString + "\"");
                }

                var kindsArr = parameters?["kinds"] as JArray;
                List<string> kinds = null;
                if (kindsArr != null && kindsArr.Count > 0)
                {
                    kinds = kindsArr.Select(t => t.ToString()).ToList();
                }

                _handler.SetParameters(nodeTypeFilter, kinds, dryRun);

                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;

                throw new TimeoutException("kg_resolve_drift timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_resolve_drift failed: {ex.Message}");
            }
        }
    }
}
