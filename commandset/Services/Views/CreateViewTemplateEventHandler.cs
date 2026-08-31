using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateViewTemplateEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int SourceViewId { get; private set; }
        public string TemplateName { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(int sourceViewId, string name)
        {
            SourceViewId = sourceViewId;
            TemplateName = name;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create View Template"))
                {
                    trans.Start();

                    View sourceView = doc.GetElement(new ElementId(SourceViewId)) as View;
                    if (sourceView == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"Source view with ID {SourceViewId} not found" };
                        return;
                    }

                    ElementId templateId = VersionCompat.CreateViewTemplate(doc, sourceView.Id);
                    View templateView = doc.GetElement(templateId) as View;

                    if (!string.IsNullOrEmpty(TemplateName) && templateView != null)
                    {
                        templateView.Name = TemplateName;
                    }

                    int resultId = templateId.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = $"View template '{templateView?.Name}' created successfully",
                        Response = resultId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating view template: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Create View Template";
    }
}
