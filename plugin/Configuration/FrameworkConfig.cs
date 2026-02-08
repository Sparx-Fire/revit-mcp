using Newtonsoft.Json;
using System.Collections.Generic;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// <para>框架配置类</para>
    /// <para>Framework configuration class.</para>
    /// </summary>
    public class FrameworkConfig
    {
        /// <summary>
        /// <para>命令配置列表</para>
        /// <para>Command configuration list.</para>
        /// </summary>
        [JsonProperty("commands")]
        public List<CommandConfig> Commands { get; set; } = new List<CommandConfig>();

        /// <summary>
        /// <para>全局设置</para>
        /// <para>Global settings.</para>
        /// </summary>
        [JsonProperty("settings")]
        public ServiceSettings Settings { get; set; } = new ServiceSettings();
    }
}
