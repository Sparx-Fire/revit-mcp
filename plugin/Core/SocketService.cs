using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.API.Interfaces;
using revit_mcp_plugin.Configuration;
using revit_mcp_plugin.Utils;

namespace revit_mcp_plugin.Core
{
    public class SocketService
    {
        private static SocketService _instance;
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private int _port = 8080;
        private UIApplication _uiApp;
        private ICommandRegistry _commandRegistry;
        private Logger _logger;
        private CommandExecutor _commandExecutor;

        public static SocketService Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new SocketService();
                return _instance;
            }
        }

        private SocketService()
        {
            _commandRegistry = new RevitCommandRegistry();
            _logger = new Logger();
        }

        public bool IsRunning => _isRunning;

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        public Logger Logger => _logger;

        // Initialization.
        public void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;

            ExternalEventManager.Instance.Initialize(uiApp, _logger);

            var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(_uiApp.Application);
            string currentVersion = versionAdapter.GetRevitVersion();
            _logger.Info("当前 Revit 版本: {0}\nCurrent Revit version: {0}", currentVersion);

            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            ConfigurationManager configManager = new ConfigurationManager(_logger);
            configManager.LoadConfiguration();

            _port = 8080;

            CommandManager commandManager = new CommandManager(
                _commandRegistry, _logger, configManager, _uiApp);
            commandManager.LoadCommands();

            _logger.Info($"Socket service initialized on port {_port}");
            _logger.Info("Command metrics log: {0}", _logger.MetricsLogFilePath);
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();
                RibbonStatusManager.UpdateStatus(_isRunning);
            }
            catch (Exception)
            {
                _isRunning = false;
                RibbonStatusManager.UpdateStatus(_isRunning);
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;

                _listener?.Stop();
                _listener = null;

                if(_listenerThread!=null && _listenerThread.IsAlive)
                {
                    _listenerThread.Join(1000);
                }

                RibbonStatusManager.UpdateStatus(_isRunning);
            }
            catch (Exception)
            {
                // log error
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    Thread clientThread = new Thread(HandleClientCommunication)
                    {
                        IsBackground = true
                    };
                    clientThread.Start(client);
                }
            }
            catch (SocketException)
            {
                
            }
            catch(Exception)
            {
                // log
            }
        }

        private void HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string response = ProcessJsonRPCRequest(message);

                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch(Exception)
            {
                // log
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private string ProcessJsonRPCRequest(string requestJson)
        {
            JsonRPCRequest request = null;
            string commandName = "unknown";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                if (request == null || !request.IsValid())
                {
                    string response = CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                    LogRequestMetrics(commandName, stopwatch, response, false, "Invalid JSON-RPC request");
                    return response;
                }

                commandName = request.Method;
                string responseJson = _commandExecutor.ExecuteCommand(request);
                bool success = IsSuccessResponse(responseJson);
                string errorDetails = success ? null : ExtractErrorDetails(responseJson);
                LogRequestMetrics(commandName, stopwatch, responseJson, success, errorDetails);

                if (string.Equals(commandName, CommandExecutor.BatchExecuteMethod, StringComparison.OrdinalIgnoreCase)
                    && success)
                {
                    LogBatchSubCommandMetrics(responseJson);
                }

                return responseJson;
            }
            catch (JsonException ex)
            {
                string response = CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
                LogRequestMetrics(commandName, stopwatch, response, false, ex.ToString());
                return response;
            }
            catch (Exception ex)
            {
                string response = CreateErrorResponse(
                    request?.Id,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}",
                    new { stackTrace = ex.ToString(), revitMessage = ex.Message }
                );
                LogRequestMetrics(commandName, stopwatch, response, false, ex.ToString());
                return response;
            }
        }

        private void LogRequestMetrics(
            string command,
            Stopwatch stopwatch,
            string responseJson,
            bool success,
            string errorDetails)
        {
            stopwatch.Stop();
            _logger.LogCommandMetrics(
                command,
                stopwatch.ElapsedMilliseconds,
                success,
                Encoding.UTF8.GetByteCount(responseJson ?? string.Empty),
                errorDetails);
        }

        private void LogBatchSubCommandMetrics(string responseJson)
        {
            try
            {
                var response = JObject.Parse(responseJson);
                var results = response["result"]?["results"] as JArray;
                if (results == null)
                    return;

                foreach (var item in results)
                {
                    var command = item["command"]?.ToString() ?? "unknown";
                    var success = item["success"]?.Value<bool>() ?? false;
                    string errorDetails = null;

                    if (!success)
                    {
                        var error = item["error"];
                        errorDetails = error?["data"]?["stackTrace"]?.ToString()
                            ?? error?["message"]?.ToString();
                    }

                    var itemJson = item.ToString(Formatting.None);
                    _logger.LogCommandMetrics(
                        $"{CommandExecutor.BatchExecuteMethod}:{command}",
                        0,
                        success,
                        Encoding.UTF8.GetByteCount(itemJson),
                        errorDetails);
                }
            }
            catch
            {
                // Metrics logging should not affect command execution.
            }
        }

        private static bool IsSuccessResponse(string responseJson)
        {
            try
            {
                var token = JObject.Parse(responseJson);
                return token["error"] == null;
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractErrorDetails(string responseJson)
        {
            try
            {
                var token = JObject.Parse(responseJson);
                var error = token["error"];
                if (error == null)
                    return null;

                var data = error["data"];
                if (data?["stackTrace"] != null)
                    return data["stackTrace"].ToString();

                return error["message"]?.ToString();
            }
            catch
            {
                return responseJson;
            }
        }

        private string CreateErrorResponse(string id, int code, string message, object data = null)
        {
            var response = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError
                {
                    Code = code,
                    Message = message,
                    Data = data != null ? JToken.FromObject(data) : null
                }
            };

            return response.ToJson();
        }
    }
}
