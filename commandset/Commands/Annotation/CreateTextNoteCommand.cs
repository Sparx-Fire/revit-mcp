using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Services.Annotation;

namespace RevitMCPCommandSet.Commands.Annotation
{
    public class CreateTextNoteCommand : ExternalEventCommandBase
    {
        private CreateTextNoteEventHandler _handler => (CreateTextNoteEventHandler)Handler;

        public override string CommandName => "create_text_note";

        public CreateTextNoteCommand(UIApplication uiApp)
            : base(new CreateTextNoteEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<TextNoteCreationInfo> data = new List<TextNoteCreationInfo>();
                data = parameters["data"].ToObject<List<TextNoteCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("创建文字注释操作超时");
            }
            catch (Exception ex)
            {
                throw new Exception($"创建文字注释失败: {ex.Message}");
            }
        }
    }
}
