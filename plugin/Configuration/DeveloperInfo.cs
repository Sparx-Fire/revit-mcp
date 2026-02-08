using Newtonsoft.Json;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// <para>开发者信息</para>
    /// <para>Developer information.</para>
    /// </summary>
    public class DeveloperInfo
    {
        /// <summary>
        /// <para>开发者名称</para>
        /// <para>Developer name.</para>
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// <para>开发者邮箱</para>
        /// <para>Developer e-mail address.</para>
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; } = "";

        /// <summary>
        /// <para>开发者网站</para>
        /// <para>Developer website.</para>
        /// </summary>
        [JsonProperty("website")]
        public string Website { get; set; } = "";

        /// <summary>
        /// <para>开发者组织</para>
        /// <para>Developer Organization.</para>
        /// </summary>
        [JsonProperty("organization")]
        public string Organization { get; set; } = "";
    }
}
