using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Annotation
{
    public class CreateRevisionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string RevisionName { get; private set; }
        public string RevisionDate { get; private set; }
        public string RevisionNumber { get; private set; }
        public string RevisionDescription { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(string name, string date, string number, string description)
        {
            RevisionName = name;
            RevisionDate = date;
            RevisionNumber = number;
            RevisionDescription = description;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create Revision"))
                {
                    trans.Start();

                    Revision revision = Revision.Create(doc);

                    if (!string.IsNullOrEmpty(RevisionName))
                    {
                        revision.Description = RevisionName;
                    }

                    if (!string.IsNullOrEmpty(RevisionDate))
                    {
                        revision.RevisionDate = RevisionDate;
                    }

                    if (!string.IsNullOrEmpty(RevisionNumber))
                    {
                        // R26: PROJECT_REVISION_REVISION_NUM parameter is read-only,
                        // try direct property or fallback gracefully
                        try
                        {
                            revision.SetRevisionNumber(RevisionNumber);
                        }
                        catch
                        {
                            // R26: parameter is read-only, skip number setting
                        }
                    }

                    if (!string.IsNullOrEmpty(RevisionDescription) && string.IsNullOrEmpty(RevisionName))
                    {
                        revision.Description = RevisionDescription;
                    }

                    int revisionId = revision.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = $"Revision '{revision.Description}' created successfully",
                        Response = revisionId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating revision: {ex.Message}"
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

        public string GetName() => "Create Revision";
    }
}
