using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class SetParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int ElementId { get; private set; }
        public JObject ParameterValues { get; private set; }
        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int elementId, JObject parameters)
        {
            ElementId = elementId;
            ParameterValues = parameters;
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
                using (var trans = new Transaction(Doc, "Set Parameters"))
                {
                    trans.Start();
                    foreach (var prop in ParameterValues.Properties())
                    {
                        var param = element.LookupParameter(prop.Name);
                        if (param == null)
                        {
                            param = LookupBuiltInParameter(element, prop.Name);
                        }
                        if (param != null && !param.IsReadOnly)
                        {
                            var value = prop.Value;
                            if (value.Type == JTokenType.String)
                                param.Set(value.Value<string>());
                            else if (value.Type == JTokenType.Integer)
                                param.Set(value.Value<int>());
                            else if (value.Type == JTokenType.Float)
                                param.Set(value.Value<double>());
                            else if (value.Type == JTokenType.Boolean)
                                param.Set(value.Value<int>());
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

        private Parameter LookupBuiltInParameter(Element element, string name)
        {
            foreach (Parameter param in element.Parameters)
            {
                var def = param.Definition;
                if (def != null)
                {
                    if (def.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return param;
                    var builtIn = def as InternalDefinition;
                    if (builtIn != null && builtIn.BuiltInParameter.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                        return param;
                }
            }
            return null;
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Set Parameters";
    }
}
