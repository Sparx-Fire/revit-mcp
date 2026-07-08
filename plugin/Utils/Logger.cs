using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;
using System;
using System.IO;

namespace revit_mcp_plugin.Utils
{
    public class Logger : ILogger
    {
        private readonly string _logFilePath;
        private readonly string _metricsLogFilePath;
        private LogLevel _currentLogLevel = LogLevel.Info;

        public Logger()
        {
            string logsDirectory = PathManager.GetLogsDirectoryPath();
            string dateStamp = DateTime.Now.ToString("yyyyMMdd");
            _logFilePath = Path.Combine(logsDirectory, $"mcp_{dateStamp}.log");
            _metricsLogFilePath = Path.Combine(logsDirectory, $"command-metrics_{dateStamp}.jsonl");
        }

        public string LogFilePath => _logFilePath;

        public string MetricsLogFilePath => _metricsLogFilePath;

        public void Log(LogLevel level, string message, params object[] args)
        {
            if (level < _currentLogLevel)
                return;

            string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {formattedMessage}";

            System.Diagnostics.Debug.WriteLine(logEntry);
            AppendToFile(_logFilePath, logEntry);
        }

        /// <summary>
        /// Logs structured command execution metrics for diagnostics and slow-command analysis.
        /// Each entry is written as a JSON line to the metrics log file.
        /// </summary>
        public void LogCommandMetrics(
            string command,
            long durationMs,
            bool success,
            int responseSize,
            string errorDetails = null)
        {
            var metrics = new
            {
                @event = "command",
                timestamp = DateTime.UtcNow.ToString("o"),
                command,
                durationMs,
                success,
                responseSize,
                error = errorDetails
            };

            string json = JsonConvert.SerializeObject(metrics);
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [METRICS] {json}";

            System.Diagnostics.Debug.WriteLine(logEntry);
            AppendToFile(_logFilePath, logEntry);
            AppendToFile(_metricsLogFilePath, json);
        }

        public void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        public void Info(string message, params object[] args)
        {
            Log(LogLevel.Info, message, args);
        }

        public void Warning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, args);
        }

        public void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, args);
        }

        private static void AppendToFile(string filePath, string content)
        {
            try
            {
                File.AppendAllText(filePath, content + Environment.NewLine);
            }
            catch
            {
                // If writing to the logfile fails, do not throw an exception.
            }
        }
    }
}
