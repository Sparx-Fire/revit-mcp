using System.Linq;
using Autodesk.Revit.UI;

namespace revit_mcp_plugin.Core
{
    public static class RibbonStatusManager
    {
        public const string PanelName = "Revit MCP Plugin";
        public const string StatusTextBoxName = "MCPStatusIndicator";

        private static UIApplication _uiApp;
        private static TextBox _statusTextBox;

        public static void RegisterStatusTextBox(TextBox statusTextBox)
        {
            _statusTextBox = statusTextBox;
        }

        public static void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;
        }

        public static void UpdateStatus(bool isRunning)
        {
            var statusBox = _statusTextBox ?? FindStatusTextBox();
            if (statusBox == null)
                return;

            statusBox.Value = isRunning ? "Connected" : "Off";
            statusBox.ToolTip = isRunning
                ? "MCP server is running and accepting connections"
                : "MCP server is stopped";
        }

        private static TextBox FindStatusTextBox()
        {
            if (_uiApp == null)
                return null;

            var panel = _uiApp.GetRibbonPanels(PanelName).FirstOrDefault();
            if (panel == null)
                return null;

            return panel.GetItems()
                .OfType<TextBox>()
                .FirstOrDefault(tb => tb.Name == StatusTextBoxName);
        }
    }
}
