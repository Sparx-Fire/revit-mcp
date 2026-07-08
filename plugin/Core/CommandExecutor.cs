using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.Exceptions;
using System;

namespace revit_mcp_plugin.Core
{
    public class CommandExecutor
    {
        public const int MaxBatchSize = 20;
        public const string BatchExecuteMethod = "batch_execute";

        private readonly ICommandRegistry _commandRegistry;
        private readonly ILogger _logger;

        public CommandExecutor(ICommandRegistry commandRegistry, ILogger logger)
        {
            _commandRegistry = commandRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Executes a Revit command declared inside a JSON-RPC request.
        /// </summary>
        /// <param name="request">A JSON-RPC request.</param>
        /// <returns></returns>
        public string ExecuteCommand(JsonRPCRequest request)
        {
            try
            {
                if (string.Equals(request.Method, BatchExecuteMethod, StringComparison.OrdinalIgnoreCase))
                {
                    return ExecuteBatch(request);
                }

                // 查找命令
                // Find command
                if (!_commandRegistry.TryGetCommand(request.Method, out var command))
                {
                    _logger.Warning("未找到命令: {0}\nCommand not found: {0}", request.Method);
                    return CreateErrorResponse(request.Id,
                        JsonRPCErrorCodes.MethodNotFound,
                        $"未找到方法: '{request.Method}'\nMethod not found: '{request.Method}'");
                }

                _logger.Info("执行命令: {0}", request.Method);

                // 执行命令
                // Execute command
                try
                {
                    object result = command.Execute(request.GetParamsObject(), request.Id);
                    _logger.Info("命令 {0} 执行成功\nCommand {0} executed successfully.", request.Method);

                    return CreateSuccessResponse(request.Id, result);
                }
                catch (CommandExecutionException ex)
                {
                    _logger.Error(
                        "命令 {0} 执行失败: {1}\nCommand {0} failed to execute: {1}\n{2}",
                        request.Method,
                        ex.Message,
                        ex.ToString());
                    var errorData = new JObject
                    {
                        ["stackTrace"] = ex.ToString(),
                        ["revitMessage"] = ex.Message
                    };
                    if (ex.ErrorData != null)
                    {
                        errorData["details"] = ex.ErrorData is JToken token
                            ? token
                            : JToken.FromObject(ex.ErrorData);
                    }
                    return CreateErrorResponse(request.Id, ex.ErrorCode, ex.Message, errorData);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        "命令 {0} 执行时发生异常: {1}\nAn exception occurred while executing command {0}: {1}\n{2}",
                        request.Method,
                        ex.Message,
                        ex.ToString());
                    return CreateErrorResponse(request.Id,
                        JsonRPCErrorCodes.InternalError,
                        ex.Message,
                        new { stackTrace = ex.ToString(), revitMessage = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("执行命令处理过程中发生异常: {0}\nAn exception has occurred durion command execution: {0}", ex.Message);
                return CreateErrorResponse(request.Id,
                    JsonRPCErrorCodes.InternalError,
                    $"内部错误: {ex.Message}\nInternal error: {ex.Message}");
            }
        }

        private string ExecuteBatch(JsonRPCRequest request)
        {
            var parameters = request.GetParamsObject() as JObject ?? new JObject();
            var commandsToken = parameters["commands"];

            if (commandsToken == null || commandsToken.Type != JTokenType.Array)
            {
                return CreateErrorResponse(
                    request.Id,
                    JsonRPCErrorCodes.InvalidParams,
                    "batch_execute requires a 'commands' array parameter");
            }

            var commands = (JArray)commandsToken;
            if (commands.Count == 0)
            {
                return CreateErrorResponse(
                    request.Id,
                    JsonRPCErrorCodes.InvalidParams,
                    "batch_execute requires at least one command");
            }

            if (commands.Count > MaxBatchSize)
            {
                return CreateErrorResponse(
                    request.Id,
                    JsonRPCErrorCodes.InvalidParams,
                    $"batch_execute exceeds maximum of {MaxBatchSize} commands");
            }

            _logger.Info("Executing batch with {0} commands", commands.Count);

            var results = new JArray();
            int succeeded = 0;
            int failed = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                var item = commands[i] as JObject;
                if (item == null)
                {
                    results.Add(CreateBatchItemResult(
                        i,
                        null,
                        false,
                        null,
                        JsonRPCErrorCodes.InvalidParams,
                        "Each batch item must be an object with 'command' and optional 'params'"));
                    failed++;
                    continue;
                }

                var commandName = item["command"]?.ToString();
                if (string.IsNullOrWhiteSpace(commandName))
                {
                    results.Add(CreateBatchItemResult(
                        i,
                        commandName,
                        false,
                        null,
                        JsonRPCErrorCodes.InvalidParams,
                        "Each batch item must include a non-empty 'command'"));
                    failed++;
                    continue;
                }

                if (string.Equals(commandName, BatchExecuteMethod, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(CreateBatchItemResult(
                        i,
                        commandName,
                        false,
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Nested batch_execute is not allowed"));
                    failed++;
                    continue;
                }

                var subParams = item["params"] as JObject ?? new JObject();
                var subRequestJson = JsonConvert.SerializeObject(new
                {
                    jsonrpc = "2.0",
                    method = commandName,
                    @params = subParams,
                    id = $"{request.Id}:{i}"
                });
                var subRequest = JsonConvert.DeserializeObject<JsonRPCRequest>(subRequestJson);

                string subResponseJson = ExecuteCommand(subRequest);
                var itemResult = ParseSubCommandResponse(i, commandName, subResponseJson);
                results.Add(itemResult);

                if (itemResult["success"]?.Value<bool>() == true)
                {
                    succeeded++;
                }
                else
                {
                    failed++;
                }
            }

            var batchResult = new JObject
            {
                ["results"] = results,
                ["summary"] = new JObject
                {
                    ["total"] = commands.Count,
                    ["succeeded"] = succeeded,
                    ["failed"] = failed
                }
            };

            _logger.Info(
                "Batch completed: {0} succeeded, {1} failed out of {2}",
                succeeded,
                failed,
                commands.Count);

            return CreateSuccessResponse(request.Id, batchResult);
        }

        private static JObject CreateBatchItemResult(
            int index,
            string command,
            bool success,
            JToken result,
            int? errorCode,
            string errorMessage,
            JToken errorData = null)
        {
            var item = new JObject
            {
                ["index"] = index,
                ["command"] = command,
                ["success"] = success
            };

            if (success)
            {
                item["result"] = result ?? JValue.CreateNull();
            }
            else
            {
                item["error"] = new JObject
                {
                    ["code"] = errorCode ?? JsonRPCErrorCodes.InternalError,
                    ["message"] = errorMessage ?? "Unknown error",
                    ["data"] = errorData ?? JValue.CreateNull()
                };
            }

            return item;
        }

        private static JObject ParseSubCommandResponse(int index, string commandName, string responseJson)
        {
            try
            {
                var response = JObject.Parse(responseJson);
                var error = response["error"];
                if (error != null)
                {
                    return CreateBatchItemResult(
                        index,
                        commandName,
                        false,
                        null,
                        error["code"]?.Value<int>(),
                        error["message"]?.ToString(),
                        error["data"]);
                }

                return CreateBatchItemResult(
                    index,
                    commandName,
                    true,
                    response["result"],
                    null,
                    null);
            }
            catch (Exception ex)
            {
                return CreateBatchItemResult(
                    index,
                    commandName,
                    false,
                    null,
                    JsonRPCErrorCodes.InternalError,
                    $"Failed to parse sub-command response: {ex.Message}");
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
