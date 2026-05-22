import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2DeleteNodeTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_delete_node",
    "Soft-delete a USER-DEFINED node (tombstone; llm_id and history preserved, recoverable). Only works on user nodes created by kg_v2_create_node — refuses Revit-projected nodes (delete those in Revit and let the projection tombstone them). Its 'contains' edges become dangling but the audit trail is kept.",
    {
      llm_id: z.string().describe("llm_id of the user node to soft-delete, e.g. 'suite_001'."),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_delete_node", { llm_id: args.llm_id });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
