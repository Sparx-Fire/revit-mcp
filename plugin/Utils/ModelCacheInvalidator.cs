using System;
using System.IO;

namespace revit_mcp_plugin.Utils
{
    public static class ModelCacheInvalidator
    {
        private static string InvalidationDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".mcp-servers-for-revit",
                "model-cache-invalidation");

        public static void InvalidateProject(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return;

            try
            {
                Directory.CreateDirectory(InvalidationDirectory);
                var safeName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(InvalidationDirectory, $"{safeName}.ts");
                File.WriteAllText(filePath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
            }
            catch
            {
                // Cache invalidation is best-effort.
            }
        }

        public static void InvalidateAll()
        {
            try
            {
                Directory.CreateDirectory(InvalidationDirectory);
                var filePath = Path.Combine(InvalidationDirectory, "_global.ts");
                File.WriteAllText(filePath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
            }
            catch
            {
                // Cache invalidation is best-effort.
            }
        }
    }
}
