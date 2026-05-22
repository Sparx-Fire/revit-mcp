import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2QueryTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_query",
    "Query KG v2 nodes by type and/or exact attrs_filter. Read-only projection of the Revit model maintained by the embedded DocumentChanged hook. " +
      "Soft-deleted nodes hidden unless include_soft_deleted=true. " +
      "COST: prefer `aggregate` for counts/means/sums/group-by (returns a scalar/table, computed server-side) instead of fetching every node and doing the math yourself; " +
      "use `select` to return only the attr fields you need (cuts payload). Reach for the full node list only when you genuinely need per-node attrs.",
    {
      node_type: z.string().optional().describe("Filter to one node type (Wall, Level, Window, ...)."),
      attrs_filter: z
        .record(z.unknown())
        .optional()
        .describe("Exact-match attrs filter (numeric coercion int↔double)."),
      include_soft_deleted: z
        .boolean()
        .optional()
        .describe("Include tombstoned nodes (default false)."),
      select: z
        .array(z.string())
        .optional()
        .describe(
          "Field projection: return only these attr keys per node (e.g. [\"height\"]). " +
            "Structural fields (llm_id, node_type, revit_id, lifecycle turns) are always included. Cuts payload."
        ),
      aggregate: z
        .object({
          op: z.enum(["count", "sum", "mean", "min", "max"]),
          field: z
            .string()
            .optional()
            .describe("Numeric attr for sum/mean/min/max (omit for count)."),
          group_by: z
            .string()
            .optional()
            .describe("Attr key (or 'node_type') to group by; returns one row per group with its value and n."),
        })
        .optional()
        .describe(
          "Server-side aggregation over the filtered nodes. Returns {op, value, n} (or {groups:[{key,value,n}]} when group_by is set) " +
            "instead of the node list. Use for 'how many walls' (count), 'mean height' (mean,field=height), " +
            "'walls per WallType' (count, group_by=type_ref) — avoids shipping the whole category."
        ),
      join: z
        .array(
          z.object({
            edge_type: z.string().describe("Edge to follow (hosts, at_level, is_type, ...)."),
            direction: z.enum(["outgoing", "incoming"]).describe("outgoing = src→dst; incoming = dst→src."),
            as: z.string().describe("Prefix for this hop's projected fields (e.g. 'host_wall', 'level')."),
            select: z.array(z.string()).optional().describe("Neighbour attr keys to project as <as>_<attr>."),
          })
        )
        .optional()
        .describe(
          "Edge-aware join projection: for each matched node, CHAIN-walk these hops (single neighbour per hop) and " +
            "flatten the result into one row per node. Each hop adds '<as>_id' plus '<as>_<attr>' for its select. " +
            "Returns `rows` instead of nodes. Use for relational audits in ONE call — e.g. Window → host Wall → Level: " +
            "join=[{edge_type:hosts,direction:incoming,as:host_wall},{edge_type:at_level,direction:outgoing,as:level,select:[name,elevation]}] " +
            "with node_type=Window, select=[sill_height] → rows of {llm_id, sill_height, host_wall_id, level_id, level_name, level_elevation}. " +
            "Avoids one traversal call per element."
        ),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_query", {
          node_type: args.node_type,
          attrs_filter: args.attrs_filter,
          include_soft_deleted: args.include_soft_deleted ?? false,
          select: args.select,
          aggregate: args.aggregate,
          join: args.join,
        });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
