using System;
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
        private ILogger _logger;
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

        // 初始化
        // Initialization.
        public void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;

            // 初始化事件管理器
            // Initialize ExternalEventManager
            ExternalEventManager.Instance.Initialize(uiApp, _logger);

            // 记录当前 Revit 版本
            // Get the current Revit version.
            var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(_uiApp.Application);
            string currentVersion = versionAdapter.GetRevitVersion();
            _logger.Info("当前 Revit 版本: {0}\nCurrent Revit version: {0}", currentVersion);



            // 创建命令执行器
            // Create CommandExecutor
            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            // 加载配置并注册命令
            // Load configuration and register commands.
            ConfigurationManager configManager = new ConfigurationManager(_logger);
            configManager.LoadConfiguration();
            

            //// 从配置中读取服务端口
            //// Read the service port from the configuration.
            //if (configManager.Config.Settings.Port > 0)
            //{
            //    _port = configManager.Config.Settings.Port;
            //}
            _port = 8080; // 固定端口号 - Hard-wired port number.

            // 加载命令
            // Load command.
            CommandManager commandManager = new CommandManager(
                _commandRegistry, _logger, configManager, _uiApp);
            commandManager.LoadCommands();

            _logger.Info($"Socket service initialized on port {_port}");
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
            }
            catch (Exception)
            {
                _isRunning = false;
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

            // 入站请求必须按完整 JSON-RPC 报文重组。TCP 是字节流：一次
            // Read 可能只返回报文的一段（大负载会跨多个 TCP 段）。旧代码
            // 把"单次 Read"当作完整请求，凡跨段 / 超过 8 KiB 的请求都解析
            // 失败并丢失（大 kg_blob_write → 客户端 120 s 超时，写入丢失）。
            // 这里累积字节直到能解析出完整 JSON-RPC 对象再处理，与 TS
            // 客户端入站逻辑对称 (SocketClient.ts processBuffer)。协议为
            // 严格的请求-应答串行（每个请求等到应答后才发下一个），故
            // "整段可解析 == 完整"成立，无需处理粘包。
            //
            // Reassemble inbound requests into complete JSON-RPC messages.
            // TCP is a byte stream: one Read may return only a fragment
            // (large payloads span several TCP segments). The old code
            // treated one Read as a whole request, so any request spanning
            // multiple segments / over 8 KiB failed to parse and was lost
            // (large kg_blob_write -> client 120 s timeout, write lost).
            // Accumulate bytes until a complete JSON-RPC object parses,
            // mirroring the TS client's inbound path. The protocol is
            // strictly serialized request->response, so "the whole buffer
            // parses" == complete (no need to split coalesced messages).
            const int ReadChunk = 8192;
            const int MaxRequestBytes = 32 * 1024 * 1024; // 防畸形请求耗尽内存
            const int PartialStallTimeoutMs = 30000;       // 半截报文迟迟不完整

            byte[] chunk = new byte[ReadChunk];

            try
            {
                using (var accumulator = new MemoryStream())
                {
                    while (_isRunning && tcpClient.Connected)
                    {
                        // 累积为空 = 持久连接空闲等待下一个请求：不设超时
                        // （否则会切断健康但空闲的复用连接）。一旦收到部分
                        // 字节，给一个有限的"完成超时"，避免半截请求永久
                        // 占用该客户端线程。
                        stream.ReadTimeout = accumulator.Length == 0
                            ? Timeout.Infinite
                            : PartialStallTimeoutMs;

                        int bytesRead;
                        try
                        {
                            bytesRead = stream.Read(chunk, 0, chunk.Length);
                        }
                        catch (IOException)
                        {
                            // 读超时（部分报文未完成）或客户端断开。
                            // Read timeout (incomplete partial) or disconnect.
                            if (accumulator.Length > 0)
                            {
                                byte[] err = Encoding.UTF8.GetBytes(
                                    CreateErrorResponse(null,
                                        JsonRPCErrorCodes.ParseError,
                                        "Incomplete JSON-RPC request (timed out " +
                                        "before a complete message arrived)"));
                                try { stream.Write(err, 0, err.Length); }
                                catch { /* client gone */ }
                            }
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            // 客户端断开连接 - Client disconnected.
                            break;
                        }

                        accumulator.Write(chunk, 0, bytesRead);

                        if (accumulator.Length > MaxRequestBytes)
                        {
                            byte[] err = Encoding.UTF8.GetBytes(
                                CreateErrorResponse(null,
                                    JsonRPCErrorCodes.InvalidRequest,
                                    $"Request exceeds {MaxRequestBytes} bytes"));
                            try { stream.Write(err, 0, err.Length); }
                            catch { /* client gone */ }
                            break;
                        }

                        // 始终解码"整个"累积字节（不是单段）：UTF-8 多字节
                        // 字符可能跨 TCP 段，逐段解码会损坏。
                        // Always decode the WHOLE accumulator (not the chunk):
                        // a UTF-8 multibyte char may straddle a segment.
                        string message = Encoding.UTF8.GetString(
                            accumulator.GetBuffer(), 0, (int)accumulator.Length);

                        // 整段能解析为合法 JSON ⇒ 完整；否则继续累积。
                        // 与 TS processBuffer 的 try-parse 行为一致。
                        if (!IsCompleteJsonRpc(message))
                        {
                            continue;
                        }

                        System.Diagnostics.Trace.WriteLine(
                            $"收到消息: {message}\nReceived message: {message}");

                        string response = ProcessJsonRPCRequest(message);

                        // 发送响应 - Send response.
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseData, 0, responseData.Length);

                        // 报文已处理，重置累积等待下一个请求（持久连接）。
                        // Message handled; reset for the next request.
                        accumulator.SetLength(0);
                    }
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

        // 整段是否构成一个完整的 JSON-RPC 请求。截断 / 括号未闭合的
        // JSON 会让 Newtonsoft 抛 JsonException ⇒ 视为"不完整"，继续
        // 累积。完整但语义非法（如缺 method）会通过解析、由
        // ProcessJsonRPCRequest 的 IsValid() 正常回错（不会挂起）。
        // 与 SocketClient.ts processBuffer 的 try-parse 对称。
        //
        // Whether the buffer is one complete JSON-RPC request. Truncated /
        // unbalanced JSON makes Newtonsoft throw JsonException => treat as
        // incomplete and keep accumulating. A complete-but-invalid request
        // (e.g. missing method) parses here and is rejected with a proper
        // error by ProcessJsonRPCRequest's IsValid() (no hang). Symmetric
        // with the TS client's processBuffer try-parse.
        private static bool IsCompleteJsonRpc(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                var probe = JsonConvert.DeserializeObject<JsonRPCRequest>(text);
                return probe != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private string ProcessJsonRPCRequest(string requestJson)
        {
            JsonRPCRequest request;

            try
            {
                // 解析JSON-RPC请求
                // Parse JSON-RPC requests.
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                // 验证请求格式是否有效
                // Verify that the request format is valid.
                if (request == null || !request.IsValid())
                {
                    return CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                }

                // 查找命令
                // Search for the command in the registry.
                if (!_commandRegistry.TryGetCommand(request.Method, out var command))
                {
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.MethodNotFound,
                        $"Method '{request.Method}' not found");
                }

                // 执行命令
                // Execute command.
                try
                {                
                    object result = command.Execute(request.GetParamsObject(), request.Id);

                    return CreateSuccessResponse(request.Id, result);
                }
                catch (Exception ex)
                {
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.InternalError, ex.Message);
                }
            }
            catch (JsonException)
            {
                // JSON解析错误
                // JSON parsing error.
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
            }
            catch (Exception ex)
            {
                // 处理请求时的其他错误
                // Catch other errors produced when processing requests.
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}"
                );
            }
        }

        private string CreateSuccessResponse(string id, object result)
        {
            var response = new JsonRPCSuccessResponse
            {
                Id = id,
                Result = result is JToken jToken ? jToken : JToken.FromObject(result)
            };

            return response.ToJson();
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
