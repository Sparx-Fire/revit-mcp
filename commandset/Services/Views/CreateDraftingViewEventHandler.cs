using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateDraftingViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string ViewName { get; private set; }
        public int Scale { get; private set; }
        public string DetailLevel { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(string name, int scale, string detailLevel)
        {
            ViewName = name;
            Scale = scale;
            DetailLevel = detailLevel;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create Drafting View"))
                {
                    trans.Start();

                    ViewFamilyType vft = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Drafting);

                    if (vft == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = "No drafting view family type found" };
                        return;
                    }

                    ViewDrafting view = ViewDrafting.Create(doc, vft.Id);

                    if (!string.IsNullOrEmpty(ViewName))
                    {
                        view.Name = ViewName;
                    }

                    if (Scale > 0)
                    {
                        view.Scale = Scale;
                    }

                    if (!string.IsNullOrEmpty(DetailLevel))
                    {
                        switch (DetailLevel.ToLowerInvariant())
                        {
                            case "coarse":
                                view.DetailLevel = ViewDetailLevel.Coarse;
                                break;
                            case "medium":
                                view.DetailLevel = ViewDetailLevel.Medium;
                                break;
                            case "fine":
                                view.DetailLevel = ViewDetailLevel.Fine;
                                break;
                        }
                    }

                    int viewId = view.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = $"Drafting view '{view.Name}' created successfully",
                        Response = viewId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating drafting view: {ex.Message}"
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

        public string GetName() => "Create Drafting View";
    }
}
