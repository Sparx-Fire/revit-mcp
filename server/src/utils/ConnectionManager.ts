import * as fs from "fs";
import * as os from "os";
import * as path from "path";
import {
  invalidateCacheForBatchResults,
  invalidateModelStatisticsCache,
  modelStatisticsCache,
  shouldInvalidateCacheForCommand,
} from "./ModelCache.js";
import { RevitClientConnection } from "./SocketClient.js";

// Mutex to serialize all Revit commands - prevents race conditions
// when multiple requests are made in parallel
let connectionMutex: Promise<void> = Promise.resolve();

const REVIT_HOST = "localhost";
const REVIT_PORT = 8080;

let sharedClient: RevitClientConnection | null = null;

const LOG_DIR = path.join(os.homedir(), ".mcp-servers-for-revit", "logs");

interface CommandMetrics {
  event: "command";
  timestamp: string;
  command: string;
  durationMs: number;
  success: boolean;
  responseSize: number;
  error?: string;
}

function ensureLogDir(): void {
  if (!fs.existsSync(LOG_DIR)) {
    fs.mkdirSync(LOG_DIR, { recursive: true });
  }
}

function getMetricsLogPath(): string {
  const dateStamp = new Date().toISOString().slice(0, 10).replace(/-/g, "");
  return path.join(LOG_DIR, `command-metrics_${dateStamp}.jsonl`);
}

export function getCommandMetricsLogPath(): string {
  ensureLogDir();
  return getMetricsLogPath();
}

function logCommandMetrics(metrics: CommandMetrics): void {
  const line = JSON.stringify(metrics);
  console.error(`[METRICS] ${line}`);

  try {
    ensureLogDir();
    fs.appendFileSync(getMetricsLogPath(), line + "\n", "utf8");
  } catch (error) {
    console.error("Failed to write command metrics log:", error);
  }
}

function wrapSendCommand(client: RevitClientConnection): void {
  const originalSendCommand = client.sendCommand.bind(client);

  client.sendCommand = async (command: string, params: any = {}) => {
    const start = Date.now();
    let responseSize = 0;

    try {
      const result = await originalSendCommand(command, params);
      responseSize = Buffer.byteLength(JSON.stringify(result ?? null), "utf8");

      modelStatisticsCache.updateLastKnownProjectNameFromResponse(result);

      if (command === "batch_execute") {
        invalidateCacheForBatchResults(result);
      } else if (shouldInvalidateCacheForCommand(command)) {
        const projectName =
          result &&
          typeof result === "object" &&
          "projectName" in result &&
          typeof (result as { projectName: unknown }).projectName === "string"
            ? (result as { projectName: string }).projectName
            : modelStatisticsCache.getLastKnownProjectName() ?? undefined;
        invalidateModelStatisticsCache(projectName);
      }

      logCommandMetrics({
        event: "command",
        timestamp: new Date().toISOString(),
        command,
        durationMs: Date.now() - start,
        success: true,
        responseSize,
      });
      return result;
    } catch (error) {
      const message =
        error instanceof Error
          ? `${error.message}${error.stack ? `\n${error.stack}` : ""}`
          : String(error);
      logCommandMetrics({
        event: "command",
        timestamp: new Date().toISOString(),
        command,
        durationMs: Date.now() - start,
        success: false,
        responseSize,
        error: message,
      });
      throw error;
    }
  };
}

function getSharedClient(): RevitClientConnection {
  if (!sharedClient) {
    sharedClient = new RevitClientConnection(REVIT_HOST, REVIT_PORT);
    sharedClient.onDisconnect(() => invalidateModelStatisticsCache());
    wrapSendCommand(sharedClient);
  }
  return sharedClient;
}

/**
 * Connect to the Revit client and execute an operation.
 * Reuses a single persistent connection for the MCP server process lifetime.
 */
export async function withRevitConnection<T>(
  operation: (client: RevitClientConnection) => Promise<T>
): Promise<T> {
  const previousMutex = connectionMutex;
  let releaseMutex: () => void;
  connectionMutex = new Promise<void>((resolve) => {
    releaseMutex = resolve;
  });
  await previousMutex;

  const revitClient = getSharedClient();

  try {
    await revitClient.ensureConnected();
    return await operation(revitClient);
  } finally {
    releaseMutex!();
  }
}

/** Closes the persistent connection (e.g. on server shutdown). */
export function closeRevitConnection(): void {
  if (sharedClient) {
    invalidateModelStatisticsCache();
    sharedClient.disconnect();
    sharedClient = null;
  }
}
