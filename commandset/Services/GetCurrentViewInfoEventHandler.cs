using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class GetCurrentViewInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // Execution result
        public CurrentViewInfo ResultInfo { get; private set; }

        // State synchronization objects
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        // IWaitableExternalEventHandler implementation
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var uiDoc = app.ActiveUIDocument;
                var doc = uiDoc.Document;
                var activeView = doc.ActiveView;

                ResultInfo = new CurrentViewInfo
                {
#if REVIT2024_OR_GREATER
                    Id = (int)activeView.Id.Value,
#else
                    Id = activeView.Id.IntegerValue,
#endif
                    UniqueId = activeView.UniqueId,
                    Name = activeView.Name,
                    ViewType = activeView.ViewType.ToString(),
                    IsTemplate = activeView.IsTemplate,
                    Scale = activeView.Scale,
                    DetailLevel = activeView.DetailLevel.ToString(),
                };
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Failed to get info");
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "Get Current View Info";
        }
    }
}
