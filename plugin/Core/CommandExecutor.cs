using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.Exceptions;
using System;

namespace revit_mcp_plugin.Core
{
    public class CommandExecutor
    {
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
                    _logger.Error("命令 {0} 执行失败: {1}\nCommand {0} failed to execute: {1}", request.Method, ex.Message);
                    return CreateErrorResponse(request.Id,
                        ex.ErrorCode,
                        ex.Message,
                        ex.ErrorData);
                }
                catch (Exception ex)
                {
                    _logger.Error("命令 {0} 执行时发生异常: {1}\nAn exception occurred while executing command {0}: {1}", request.Method, ex.Message);
                    return CreateErrorResponse(request.Id,
                        JsonRPCErrorCodes.InternalError,
                        ex.Message);
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
