import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2GetByRevitIdTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_get_by_revit_id",
    "Resolve a Revit ElementId to its KG v2 node (or {found:false} if unbound). Use after a create/select Revit op to retrieve the projected node + its KG attrs.",
    {
      revit_id: z
        .number()
        .describe("Revit Element id (long). Returned by create_* ops or ai_element_filter."),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_get_by_revit_id", {
          revit_id: args.revit_id,
        });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
