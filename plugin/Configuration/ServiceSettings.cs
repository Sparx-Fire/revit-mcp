using Newtonsoft.Json;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// <para>服务设置类</para>
    /// <para>Service settings.</para>
    /// </summary>
    public class ServiceSettings
    {
        /// <summary>
        /// <para>日志级别</para>
        /// <para>Log level.</para>
        /// </summary>
        [JsonProperty("logLevel")]
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// <para>socket服务端口</para>
        /// <para>Socket service port.</para>
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; } = 8080;

        /// <summary>
        /// <para>Автозапуск MCP-сервера при открытии Revit</para>
        /// <para>Automatically start the MCP server when Revit opens.</para>
        /// </summary>
        [JsonProperty("autoStartOnLaunch")]
        public bool AutoStartOnLaunch { get; set; } = true;

    }
}
