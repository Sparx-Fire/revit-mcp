using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// <para>命令配置类</para>
    /// <para>Command configuration class.</para>
    /// </summary>
    public class CommandConfig
    {
        /// <summary>
        /// <para>命令名称 - 对应IRevitCommand.CommandName</para>
        /// <para>Name of the command. Corresponds to <see cref="IRevitCommand.CommandName"/></para>
        /// </summary>
        [JsonProperty("commandName")]
        public string CommandName { get; set; }

        /// <summary>
        /// <para>程序集路径 - 包含此命令的DLL</para>
        /// <para>Assembly path - DLL containing this command.</para>
        /// </summary>
        [JsonProperty("assemblyPath")]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// <para>是否启用该命令</para>
        /// <para>Enable this command.</para>
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// <para>支持的Revit版本</para>
        /// <para>Supported Revit versions.</para>
        /// </summary>
        [JsonProperty("supportedRevitVersions")]
        public string[] SupportedRevitVersions { get; set; } = new string[0];

        /// <summary>
        /// <para>开发者信息</para>
        /// <para>Developer information.</para>
        /// </summary>
        [JsonProperty("developer")]
        public DeveloperInfo Developer { get; set; } = new DeveloperInfo();

        /// <summary>
        /// <para>命令描述</para>
        /// <para>Command description.</para>
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = "";
    }
}
