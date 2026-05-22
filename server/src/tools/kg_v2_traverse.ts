import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

const StepSchema = z.object({
  edge_type: z.string().describe("at_level | is_type | hosts | bounded_by | connects_at | derived_from"),
  direction: z
    .enum(["out", "in"])
    .optional()
    .describe("'out' = follow edge from src→dst (default), 'in' = reverse"),
});

export function registerKgV2TraverseTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_traverse",
    "Traversal in the KG v2 from start_id. TWO modes: " +
      "(1) fixed PATH — `path` of {edge_type, direction} steps, returns the set reached after the last hop. " +
      "(2) variable-depth REACHABILITY — give `edge_types` + `max_depth` (+ `direction` out|in|any): BFS that follows ANY of those edge types up to max_depth hops and returns every node reached with its `depth`. " +
      "Use reachability for cascade/blast-radius (e.g. level → walls → windows: edge_types=[at_level,hosts], direction=in), dependency fan-out, and provenance chains. " +
      "For provenance through DELETED predecessors (replaced_by chains), set include_soft_deleted=true. Soft-deleted nodes are otherwise not traversed in reachability mode.",
    {
      start_id: z.string().describe("Starting llm_id (e.g. wall_001, level_001)."),
      path: z
        .array(StepSchema)
        .optional()
        .describe("PATH mode: ordered edge-following steps. Empty/omitted → start only (unless reachability params are given)."),
      edge_types: z
        .array(z.string())
        .optional()
        .describe("REACHABILITY mode: edge types to follow (any of them), e.g. [at_level, hosts] or [replaced_by]."),
      direction: z
        .enum(["out", "in", "any"])
        .optional()
        .describe("REACHABILITY direction (default any): out = src→dst, in = dst→src, any = both."),
      max_depth: z
        .number()
        .int()
        .optional()
        .describe("REACHABILITY max BFS hops from start (default 8, capped 64). Presence of this OR edge_types selects reachability mode."),
      include_soft_deleted: z
        .boolean()
        .optional()
        .describe("REACHABILITY: traverse through tombstoned nodes (default false). Set true for replaced_by provenance across deletions."),
    },
    async (args: any) => {
      try {
        const reachability = args.edge_types !== undefined || args.max_depth !== undefined;
        const params: Record<string, unknown> = { start_id: args.start_id };
        if (reachability) {
          params.edge_types = args.edge_types ?? [];
          params.max_depth = args.max_depth ?? 8;
          params.direction = args.direction ?? "any";
          params.include_soft_deleted = args.include_soft_deleted ?? false;
        } else {
          params.path = args.path ?? [];
        }
        const r = await callV2<unknown>("kg_traverse", params);
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
