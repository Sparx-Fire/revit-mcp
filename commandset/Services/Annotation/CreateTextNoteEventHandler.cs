using RevitMCPCommandSet.Models.Annotation;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Annotation
{
    public class CreateTextNoteEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<TextNoteCreationInfo> CreatedInfo { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<TextNoteCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var data in CreatedInfo)
                {
                    XYZ location = JZPoint.ToXYZ(data.Location);

                    View view = null;
                    if (data.ViewId != -1 && data.ViewId != 0)
                    {
                        view = doc.GetElement(new ElementId(data.ViewId)) as View;
                    }

                    if (view == null)
                    {
                        view = doc.ActiveView;
                    }

                    TextNoteType textNoteType = null;
                    if (data.TextNoteTypeId != -1 && data.TextNoteTypeId != 0)
                    {
                        Element typeElem = doc.GetElement(new ElementId(data.TextNoteTypeId));
                        if (typeElem != null && typeElem is TextNoteType)
                        {
                            textNoteType = typeElem as TextNoteType;
                        }
                    }

                    if (textNoteType == null)
                    {
                        textNoteType = new FilteredElementCollector(doc)
                            .OfClass(typeof(TextNoteType))
                            .Cast<TextNoteType>()
                            .FirstOrDefault();

                        if (data.TextNoteTypeId != -1 && data.TextNoteTypeId != 0)
                        {
                            _warnings.Add($"Requested text note typeId {data.TextNoteTypeId} not found. Defaulted to '{textNoteType?.Name}' (ID: {textNoteType?.Id.GetIntValue()})");
                        }
                    }

                    if (textNoteType == null)
                    {
                        _warnings.Add("No text note types available in project.");
                        continue;
                    }

                    using (Transaction trans = new Transaction(doc, "创建文字注释"))
                    {
                        trans.Start();

                        TextNote textNote = TextNote.Create(doc, view.Id, location, data.Text, textNoteType.Id);

                        if (textNote != null)
                        {
                            if (data.Rotation != 0)
                            {
                                VersionCompat.SetTextNoteRotation(doc, textNote, data.Rotation);
                            }

                            if (data.Width > 0)
                            {
                                textNote.Width = data.Width / 304.8;
                            }

                            if (data.HorizontalAlign != 0)
                            {
                                textNote.HorizontalAlignment = (HorizontalTextAlignment)data.HorizontalAlign;
                            }

                            if (data.VerticalAlign != 0)
                            {
                                textNote.VerticalAlignment = (VerticalTextAlignment)data.VerticalAlign;
                            }

                            elementIds.Add(textNote.Id.GetIntValue());
                        }

                        trans.Commit();
                    }
                }

                string message = $"Successfully created {elementIds.Count} text note(s).";
                if (_warnings.Count > 0)
                {
                    message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = elementIds,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"创建文字注释时出错: {ex.Message}",
                };
                TaskDialog.Show("错误", $"创建文字注释时出错: {ex.Message}");
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create Text Note";
        }
    }
}
