using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.DataExtraction;
using RevitMCPCommandSet.Services.DataExtraction;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.DataExtraction
{
    public abstract class CreateScheduleDataCommandBase : ExternalEventCommandBase
    {
        private CreateScheduleDataEventHandler _handler => (CreateScheduleDataEventHandler)Handler;

        protected abstract ScheduleElementCategory Category { get; }

        protected CreateScheduleDataCommandBase(UIApplication uiApp)
            : base(new CreateScheduleDataEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                _handler.SetParameters(Category);

                if (RaiseAndWaitForCompletion(120000))
                {
                    return _handler.ResultInfo;
                }

                throw new TimeoutException($"Export {Category.ToString().ToLowerInvariant()} schedule timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export {Category.ToString().ToLowerInvariant()} schedule: {ex.Message}");
            }
        }
    }

    public class CreateDoorScheduleCommand : CreateScheduleDataCommandBase
    {
        public override string CommandName => "create_door_schedule";

        protected override ScheduleElementCategory Category => ScheduleElementCategory.Doors;

        public CreateDoorScheduleCommand(UIApplication uiApp) : base(uiApp) { }
    }

    public class CreateWindowScheduleCommand : CreateScheduleDataCommandBase
    {
        public override string CommandName => "create_window_schedule";

        protected override ScheduleElementCategory Category => ScheduleElementCategory.Windows;

        public CreateWindowScheduleCommand(UIApplication uiApp) : base(uiApp) { }
    }

    public class CreateFloorScheduleCommand : CreateScheduleDataCommandBase
    {
        public override string CommandName => "create_floor_schedule";

        protected override ScheduleElementCategory Category => ScheduleElementCategory.Floors;

        public CreateFloorScheduleCommand(UIApplication uiApp) : base(uiApp) { }
    }
}
