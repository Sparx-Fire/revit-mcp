import * as net from "net";
export class RevitClientConnection {
    host;
    port;
    socket;
    isConnected = false;
    responseCallbacks = new Map();
    buffer = "";
    constructor(host, port) {
        this.host = host;
        this.port = port;
        this.socket = new net.Socket();
        this.setupSocketListeners();
    }
    setupSocketListeners() {
        this.socket.on("connect", () => {
            this.isConnected = true;
        });
        this.socket.on("data", (data) => {
            // 将接收到的数据添加到缓冲区
            const dataString = data.toString();
            this.buffer += dataString;
            // 尝试解析完整的JSON响应
            this.processBuffer();
        });
        this.socket.on("close", () => {
            this.isConnected = false;
        });
        this.socket.on("error", (error) => {
            console.error("RevitClientConnection error:", error);
            this.isConnected = false;
        });
    }
    processBuffer() {
        try {
            // 尝试解析JSON
            const response = JSON.parse(this.buffer);
            // 如果成功解析，处理响应并清空缓冲区
            this.handleResponse(this.buffer);
            this.buffer = "";
        }
        catch (e) {
            // 如果解析失败，可能是因为数据不完整，继续等待更多数据
        }
    }
    connect() {
        if (this.isConnected) {
            return true;
        }
        try {
            this.socket.connect(this.port, this.host);
            return true;
        }
        catch (error) {
            console.error("Failed to connect:", error);
            return false;
        }
    }
    disconnect() {
        this.socket.end();
        this.isConnected = false;
    }
    generateRequestId() {
        return Date.now().toString() + Math.random().toString().substring(2, 8);
    }
    handleResponse(responseData) {
        try {
            const response = JSON.parse(responseData);
            // 从响应中获取ID
            const requestId = response.id || "default";
            const callback = this.responseCallbacks.get(requestId);
            if (callback) {
                callback(responseData);
                this.responseCallbacks.delete(requestId);
            }
        }
        catch (error) {
            console.error("Error parsing response:", error);
        }
    }
    sendCommand(command, params = {}) {
        return new Promise((resolve, reject) => {
            try {
                if (!this.isConnected) {
                    this.connect();
                }
                // 生成请求ID
                const requestId = this.generateRequestId();
                // 创建符合JSON-RPC标准的请求对象
                const commandObj = {
                    jsonrpc: "2.0",
                    method: command,
                    params: params,
                    id: requestId,
                };
                // 存储回调函数
                this.responseCallbacks.set(requestId, (responseData) => {
                    try {
                        const response = JSON.parse(responseData);
                        if (response.error) {
                            reject(new Error(response.error.message || "Unknown error from Revit"));
                        }
                        else {
                            resolve(response.result);
                        }
                    }
                    catch (error) {
                        if (error instanceof Error) {
                            reject(new Error(`Failed to parse response: ${error.message}`));
                        }
                        else {
                            reject(new Error(`Failed to parse response: ${String(error)}`));
                        }
                    }
                });
                // 发送命令
                const commandString = JSON.stringify(commandObj);
                this.socket.write(commandString);
                // 设置超时
                setTimeout(() => {
                    if (this.responseCallbacks.has(requestId)) {
                        this.responseCallbacks.delete(requestId);
                        reject(new Error(`Command timed out after 2 minutes: ${command}`));
                    }
                }, 120000); // 2分钟超时
            }
            catch (error) {
                reject(error);
            }
        });
    }
}
