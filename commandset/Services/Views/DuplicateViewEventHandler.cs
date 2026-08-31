using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class DuplicateViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ViewId { get; private set; }
        public string Mode { get; private set; }
        public string NewName { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(int viewId, string mode, string newName)
        {
            ViewId = viewId;
            Mode = mode;
            NewName = newName;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Duplicate View"))
                {
                    trans.Start();

                    View view = doc.GetElement(new ElementId(ViewId)) as View;
                    if (view == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"View with ID {ViewId} not found" };
                        return;
                    }

                    ViewDuplicateOption option;
                    switch (Mode.ToLowerInvariant())
                    {
                        case "duplicate":
                            option = ViewDuplicateOption.Duplicate;
                            break;
                        case "with_detailing":
                            option = ViewDuplicateOption.WithDetailing;
                            break;
                        case "dependent":
                            option = VersionCompat.GetDependentDuplicateOption();
                            break;
                        default:
                            option = ViewDuplicateOption.Duplicate;
                            break;
                    }

                    ElementId newViewId = view.Duplicate(option);
                    View newView = doc.GetElement(newViewId) as View;

                    if (!string.IsNullOrEmpty(NewName) && newView != null)
                    {
                        newView.Name = NewName;
                    }

                    int resultId = newViewId.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = $"View duplicated successfully with mode '{Mode}'",
                        Response = resultId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error duplicating view: {ex.Message}"
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

        public string GetName() => "Duplicate View";
    }
}
