import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

// Built-in F2 annotation kinds. `kind` is NOT restricted to these: any
// name that is not a Revit-owned F1 edge type (at_level, is_type, hosts,
// bounded_by, connects_at, derived_from) is accepted and registered as a
// USER-DEFINED edge type on first use — symmetric to user-defined node
// types via kg_v2_create_node. Validation (incl. F1 rejection) is enforced
// server-side in ProjectKg.Annotate.
const BUILTIN_KINDS = [
  "replaced_by",
  "tagged",
  "violates_rule",
  "implements_intent",
  "contains",
] as const;

const KindSchema = z
  .string()
  .min(1)
  .refine((k) => k.trim().length > 0, "kind must be non-empty");

export function registerKgV2AnnotateTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_annotate",
    "Author / replace / delete a semantic (F2 / KG-owned) edge between two KG nodes. Built-in kinds: replaced_by (audit trail for Rebind), tagged (free-form labels), violates_rule (KB compliance pointer), implements_intent (design intent registry pointer), contains (membership: a user-defined node such as a Suite groups existing nodes like Rooms — src=container, dst=member). USER-DEFINED edge types: pass any other name (e.g. 'adjacent_to', 'serves', 'feeds') and it is registered as a user edge type on first use — symmetric to user-defined node types (kg_v2_create_node). Revit-owned F1 edge types (at_level, is_type, hosts, bounded_by, connects_at, derived_from) are rejected — they are maintained by the projection. User/F2 edges are never repatched by the Revit projection and survive undo/redo (§6). Pass a payload object to upsert/replace ; omit payload or pass null to delete the edge if it exists (idempotent, no-op otherwise). Accepts soft-deleted src/dst. Traverse/query them by edge_type like any edge.",
    {
      kind: KindSchema.describe(`Edge kind. Built-in: ${BUILTIN_KINDS.join(" | ")}. Or any other name → user-defined edge type (registered on first use; F1 Revit-owned types rejected).`),
      src: z.string().describe("Source llm_id (may be soft-deleted)."),
      dst: z.string().describe("Destination llm_id (may be soft-deleted)."),
      payload: z
        .record(z.unknown())
        .nullable()
        .optional()
        .describe("Free-form annotation payload object ; null/omitted = delete the edge if present."),
    },
    async (args: any) => {
      try {
        const params: Record<string, unknown> = {
          kind: args.kind,
          src: args.src,
          dst: args.dst,
        };
        if (args.payload !== undefined) params.payload = args.payload;
        const r = await callV2<unknown>("kg_annotate", params);
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
