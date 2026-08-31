using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class RenameElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int ElementId { get; private set; }
        public string NewName { get; private set; }
        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int elementId, string newName)
        {
            ElementId = elementId;
            NewName = newName;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                var element = Doc.GetElement(new ElementId(ElementId));
                if (element == null)
                {
                    Result = new AIResult<bool> { Success = false, Message = $"Element {ElementId} not found" };
                    return;
                }
                using (var trans = new Transaction(Doc, "Rename Element"))
                {
                    trans.Start();
                    bool renamed = false;
                    foreach (Parameter param in element.Parameters)
                    {
                        var def = param.Definition;
                        if (def != null && (def.Name == "名称" || def.Name == "Name"))
                        {
                            if (!param.IsReadOnly)
                            {
                                param.Set(NewName);
                                renamed = true;
                            }
                            break;
                        }
                    }
                    if (!renamed)
                    {
                        if (element is Level level)
                        {
                            level.Name = NewName;
                            renamed = true;
                        }
                        else if (element is Grid grid)
                        {
                            grid.Name = NewName;
                            renamed = true;
                        }
                        else if (element is ElementType elemType)
                        {
                            elemType.Name = NewName;
                            renamed = true;
                        }
                    }
                    trans.Commit();
                }
                Result = new AIResult<bool> { Success = true, Response = true };
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Rename Element";
    }
}
