import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2SessionInfoTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_session_info",
    "Return KG v2 session metadata: project_id, current turn, node/edge counts, pending delta count, ES journal length, last action summary. Read-only. Use as a heartbeat / sanity check.",
    {},
    async () => {
      try {
        const r = await callV2<unknown>("kg_session_info", {});
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
