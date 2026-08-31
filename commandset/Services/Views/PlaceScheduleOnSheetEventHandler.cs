using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class PlaceScheduleOnSheetEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ScheduleId { get; private set; }
        public int SheetId { get; private set; }
        public double LocationX { get; private set; }
        public double LocationY { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(int scheduleId, int sheetId, double x, double y)
        {
            ScheduleId = scheduleId;
            SheetId = sheetId;
            LocationX = x;
            LocationY = y;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Place Schedule on Sheet"))
                {
                    trans.Start();

                    ElementId scheduleElemId = new ElementId(ScheduleId);
                    ElementId sheetElemId = new ElementId(SheetId);

                    ViewSchedule schedule = doc.GetElement(scheduleElemId) as ViewSchedule;
                    if (schedule == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"Schedule with ID {ScheduleId} not found" };
                        return;
                    }

                    ViewSheet sheet = doc.GetElement(sheetElemId) as ViewSheet;
                    if (sheet == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"Sheet with ID {SheetId} not found" };
                        return;
                    }

                    XYZ point = new XYZ(LocationX / 304.8, LocationY / 304.8, 0);
                    ScheduleSheetInstance instance = ScheduleSheetInstance.Create(doc, sheet.Id, schedule.Id, point);

                    int instanceId = instance.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = "Schedule placed on sheet successfully",
                        Response = instanceId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error placing schedule on sheet: {ex.Message}"
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

        public string GetName() => "Place Schedule on Sheet";
    }
}
