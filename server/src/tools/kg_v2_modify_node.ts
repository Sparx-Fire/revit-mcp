import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2ModifyNodeTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_modify_node",
    "Modify attrs of a USER-DEFINED node (free-form merge: keys in updates overwrite, others kept). Only works on user nodes created by kg_v2_create_node — refuses Revit-projected nodes (edit those in Revit; the projection updates the KG automatically). Records a modify entry in the action log.",
    {
      llm_id: z.string().describe("llm_id of the user node to modify, e.g. 'suite_001'."),
      updates: z
        .record(z.unknown())
        .describe("Attribute keys to set/overwrite (free-form)."),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_modify_node", {
          llm_id: args.llm_id,
          updates: args.updates,
        });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
