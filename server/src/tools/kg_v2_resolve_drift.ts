import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

const KIND_VALUES = [
  "missing_in_kg",
  "orphan_kg_node",
  "tombstoned_but_live",
  "attrs_diverged",
] as const;

export function registerKgV2ResolveDriftTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_resolve_drift",
    "Align the KG v2 projection to the live Revit document by applying per-entry resolutions: 'missing_in_kg' projects the Revit element into the KG (new node, created_at_turn>0); 'attrs_diverged' overwrites the KG attrs from Revit; 'orphan_kg_node' soft-deletes the KG node (history kept, queryable via include_soft_deleted); 'tombstoned_but_live' resurrects the soft-deleted node and re-syncs attrs. action_log entries are appended, F2 annotations (replaced_by / tagged / ...) and bootstrap nodes (created_at_turn=0) are preserved. Optional filters: kinds (subset of the 4 kinds), node_type (restrict to one type). dry_run=true reports what would be done without mutating. Non-dry-run REQUIRES confirm=\"align-to-revit\".",
    {
      kinds: z
        .array(z.enum(KIND_VALUES))
        .optional()
        .describe(
          "Subset of drift kinds to resolve. Default = all 4. Entries of other kinds end up in unresolved with reason 'filtered_by_kinds'.",
        ),
      node_type: z
        .string()
        .optional()
        .describe("Restrict drift scan to a single node type (Level, WallType, Wall, Window, Door, FamilyType)."),
      dry_run: z
        .boolean()
        .optional()
        .describe("If true, report what would be resolved without mutating. Default false."),
      confirm: z
        .string()
        .optional()
        .describe("Mandatory if dry_run=false. Must equal 'align-to-revit'."),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_resolve_drift", {
          kinds: args.kinds,
          node_type: args.node_type,
          dry_run: args.dry_run ?? false,
          confirm: args.confirm,
        });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
