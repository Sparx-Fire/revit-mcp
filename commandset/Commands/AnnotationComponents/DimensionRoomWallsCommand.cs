using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Services.AnnotationComponents;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.AnnotationComponents;

public class DimensionRoomWallsCommand : ExternalEventCommandBase
{
    private DimensionRoomWallsEventHandler _handler => (DimensionRoomWallsEventHandler)Handler;

    public DimensionRoomWallsCommand(UIApplication uiApp)
        : base(new DimensionRoomWallsEventHandler(), uiApp)
    {
    }

    public override string CommandName => "dimension_room_walls";

    public override object Execute(JObject parameters, string requestId)
    {
        try
        {
            var info = parameters?.ToObject<RoomWallDimensionInfo>()
                ?? throw new ArgumentException("Room wall dimension parameters are required.");

            if (info.RoomId <= 0)
                throw new ArgumentException("roomId must be a positive element ID.");

            _handler.SetParameters(info);

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;

            throw new TimeoutException("Room wall dimension operation timed out after 30 seconds.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating room wall dimensions: {ex.Message}", ex);
        }
    }
}
