import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2CreateNodeTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_create_node",
    "Create a USER-DEFINED semantic node that is NOT derived from the Revit model — e.g. Suite, Apartment, Zone, FireCompartment. The node_type is registered on first use and must not collide with a built-in Revit-projected type (Wall, Room, Level, Window, Door, WallType, FamilyType, ...). Attrs are free-form (no schema). The node carries no revit_id, so it is invisible to drift detection and untouched by rescan/projection — it persists across sessions via the delta log. Returns the new llm_id; group existing nodes under it with kg_v2_annotate kind=contains (e.g. a Suite that contains several Rooms).",
    {
      node_type: z
        .string()
        .min(1)
        .describe("User type name, e.g. 'Suite'. Must not be a built-in Revit-projected type."),
      attrs: z
        .record(z.unknown())
        .optional()
        .describe("Free-form attribute object (no schema), e.g. {name:'Suite A', program:'residential'}."),
    },
    async (args: any) => {
      try {
        const params: Record<string, unknown> = { node_type: args.node_type };
        if (args.attrs !== undefined) params.attrs = args.attrs;
        const r = await callV2<unknown>("kg_create_node", params);
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
