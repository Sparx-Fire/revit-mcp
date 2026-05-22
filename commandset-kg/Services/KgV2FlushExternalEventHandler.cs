using Autodesk.Revit.UI;

namespace RevitMCPKgCommandSet.Services
{
    public sealed class KgV2FlushExternalEventHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            try { KgV2DocumentWatcher.FlushCurrent(); }
            catch { }
        }

        public string GetName() => "KG v2 flush";
    }
}
