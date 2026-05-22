import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2DetectDriftTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_detect_drift",
    "Compare the KG v2 projection against the active Revit document and return divergences. Kinds: 'missing_in_kg' (element in Revit but no KG node), 'orphan_kg_node' (live KG node whose revit_id no longer exists), 'tombstoned_but_live' (KG soft-deleted yet Revit element alive), 'attrs_diverged' (live KG attrs disagree with the current Revit values, with kg_attrs / revit_attrs side-by-side). Optional node_type filter restricts the scan. Read-only.",
    {
      node_type: z
        .string()
        .optional()
        .describe("Restrict drift scan to a single node type (Level, WallType, Wall, Window, Door, FamilyType)."),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_detect_drift", {
          node_type: args.node_type,
        });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
