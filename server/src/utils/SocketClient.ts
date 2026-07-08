import * as net from "net";

const CONNECT_TIMEOUT_MS = 5000;
const COMMAND_TIMEOUT_MS = 120000;

export class RevitClientConnection {
  host: string;
  port: number;
  socket: net.Socket;
  isConnected: boolean = false;
  private intentionallyClosed: boolean = false;
  private connectPromise: Promise<void> | null = null;
  private disconnectCallbacks: Array<() => void> = [];
  responseCallbacks: Map<string, (response: string) => void> = new Map();
  buffer: string = "";

  constructor(host: string, port: number) {
    this.host = host;
    this.port = port;
    this.socket = this.createSocket();
  }

  private createSocket(): net.Socket {
    const socket = new net.Socket();
    this.setupSocketListeners(socket);
    return socket;
  }

  private setupSocketListeners(socket: net.Socket): void {
    socket.on("connect", () => {
      this.isConnected = true;
    });

    socket.on("data", (data) => {
      const dataString = data.toString();
      this.buffer += dataString;
      this.processBuffer();
    });

    socket.on("close", () => {
      this.handleDisconnect();
    });

    socket.on("error", (error) => {
      console.error("RevitClientConnection error:", error);
      this.handleDisconnect();
    });
  }

  public onDisconnect(callback: () => void): void {
    this.disconnectCallbacks.push(callback);
  }

  private notifyDisconnect(): void {
    for (const callback of this.disconnectCallbacks) {
      callback();
    }
  }

  private handleDisconnect(): void {
    this.notifyDisconnect();
    this.isConnected = false;
    this.connectPromise = null;
    this.buffer = "";

    for (const [requestId, callback] of this.responseCallbacks) {
      callback(
        JSON.stringify({
          jsonrpc: "2.0",
          id: requestId,
          error: {
            code: -32000,
            message: "Connection to Revit lost",
          },
        })
      );
    }
    this.responseCallbacks.clear();
  }

  private resetSocket(): void {
    this.socket.removeAllListeners();
    if (!this.socket.destroyed) {
      this.socket.destroy();
    }
    this.socket = this.createSocket();
    this.isConnected = false;
    this.connectPromise = null;
    this.buffer = "";
  }

  public connect(): Promise<void> {
    if (this.isConnected) {
      return Promise.resolve();
    }

    if (this.connectPromise) {
      return this.connectPromise;
    }

    if (this.socket.destroyed) {
      this.resetSocket();
    }

    this.connectPromise = new Promise<void>((resolve, reject) => {
      const onConnect = () => {
        cleanup();
        this.isConnected = true;
        resolve();
      };

      const onError = (error: Error) => {
        cleanup();
        this.isConnected = false;
        this.connectPromise = null;
        reject(
          new Error(`Failed to connect to Revit client: ${error.message}`)
        );
      };

      const onTimeout = () => {
        cleanup();
        this.isConnected = false;
        this.connectPromise = null;
        this.socket.destroy();
        reject(new Error("连接到Revit客户端失败"));
      };

      const cleanup = () => {
        clearTimeout(timeoutId);
        this.socket.removeListener("connect", onConnect);
        this.socket.removeListener("error", onError);
      };

      this.socket.once("connect", onConnect);
      this.socket.once("error", onError);

      try {
        this.socket.connect(this.port, this.host);
      } catch (error) {
        cleanup();
        this.connectPromise = null;
        reject(error);
        return;
      }

      const timeoutId = setTimeout(onTimeout, CONNECT_TIMEOUT_MS);
    });

    return this.connectPromise;
  }

  public async ensureConnected(): Promise<void> {
    if (this.intentionallyClosed) {
      throw new Error("Revit connection was closed");
    }

    if (this.isConnected && !this.socket.destroyed) {
      return;
    }

    if (this.socket.destroyed) {
      this.resetSocket();
    }

    await this.connect();
  }

  public disconnect(): void {
    this.intentionallyClosed = true;
    this.connectPromise = null;
    this.isConnected = false;
    this.buffer = "";

    for (const [requestId, callback] of this.responseCallbacks) {
      callback(
        JSON.stringify({
          jsonrpc: "2.0",
          id: requestId,
          error: {
            code: -32000,
            message: "Connection to Revit closed",
          },
        })
      );
    }
    this.responseCallbacks.clear();

    if (!this.socket.destroyed) {
      this.socket.end();
      this.socket.destroy();
    }
  }

  private processBuffer(): void {
    try {
      JSON.parse(this.buffer);
      this.handleResponse(this.buffer);
      this.buffer = "";
    } catch {
      // Incomplete JSON — wait for more data
    }
  }

  private generateRequestId(): string {
    return Date.now().toString() + Math.random().toString().substring(2, 8);
  }

  private handleResponse(responseData: string): void {
    try {
      const response = JSON.parse(responseData);
      const requestId = response.id || "default";

      const callback = this.responseCallbacks.get(requestId);
      if (callback) {
        callback(responseData);
        this.responseCallbacks.delete(requestId);
      }
    } catch (error) {
      console.error("Error parsing response:", error);
    }
  }

  public async sendCommand(command: string, params: any = {}): Promise<any> {
    await this.ensureConnected();

    return new Promise((resolve, reject) => {
      try {
        const requestId = this.generateRequestId();

        const commandObj = {
          jsonrpc: "2.0",
          method: command,
          params: params,
          id: requestId,
        };

        this.responseCallbacks.set(requestId, (responseData) => {
          try {
            const response = JSON.parse(responseData);
            if (response.error) {
              reject(
                new Error(response.error.message || "Unknown error from Revit")
              );
            } else {
              resolve(response.result);
            }
          } catch (error) {
            if (error instanceof Error) {
              reject(new Error(`Failed to parse response: ${error.message}`));
            } else {
              reject(new Error(`Failed to parse response: ${String(error)}`));
            }
          }
        });

        const commandString = JSON.stringify(commandObj);
        this.socket.write(commandString);

        setTimeout(() => {
          if (this.responseCallbacks.has(requestId)) {
            this.responseCallbacks.delete(requestId);
            reject(new Error(`Command timed out after 2 minutes: ${command}`));
          }
        }, COMMAND_TIMEOUT_MS);
      } catch (error) {
        reject(error);
      }
    });
  }
}
