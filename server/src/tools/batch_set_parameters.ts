import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerBatchSetParametersTool(server: McpServer) {
  server.tool(
    "batch_set_parameters",
    "Apply N parameter mutations on N elements in a SINGLE Revit Transaction. " +
    "O(1) Transaction overhead instead of O(N) when calling operate_element / " +
    "send_code_to_revit per element. Use for: bulk height/sill/comment/mark " +
    "edits, type swaps (param = 'ELEM_FAMILY_AND_TYPE_PARAM' or 'Type', value " +
    "= the target ElementId), grouped color/transparency changes, etc. " +
    "Length params (height, sill_height, ...) accept METRES on the wire and " +
    "auto-convert to Revit's internal feet. Atomic rollback by default. " +
    "Returns succeeded/failed counts + per-error details. Revit Warnings " +
    "(e.g. 'walls overlap') are silently swallowed at the txn level — only " +
    "Errors halt the batch.",
    {
      data: z
        .object({
          operations: z
            .array(
              z.object({
                element_id: z
                  .number()
                  .int()
                  .describe("Revit ElementId (Int64)."),
                param: z
                  .string()
                  .describe(
                    "Parameter identifier. Resolved in order: (1) BuiltInParameter " +
                    "enum name like 'WALL_USER_HEIGHT_PARAM' / " +
                    "'INSTANCE_SILL_HEIGHT_PARAM', (2) the human-readable parameter " +
                    "name as it appears in Revit's Properties panel (case-insensitive). " +
                    "The first match wins."
                  ),
                value: z
                  .union([z.number(), z.string(), z.boolean()])
                  .describe(
                    "New value. For length-typed parameters (height, sill_height, " +
                    "etc.), pass METRES — they're auto-converted to Revit's internal " +
                    "feet. For non-length numerics, pass raw per the parameter's " +
                    "expected unit. For text params, pass a string."
                  ),
              })
            )
            .min(1)
            .describe(
              "List of {element_id, param, value} operations to apply in a single " +
              "Revit Transaction. Order is preserved; per-op failures are captured " +
              "in the 'errors' field of the result."
            ),
          atomic: z
            .boolean()
            .default(true)
            .describe(
              "If true (default), any per-op failure rolls back the WHOLE batch — " +
              "no partial state. If false, best-effort: failed ops are skipped " +
              "and reported; succeeded ops commit."
            ),
        })
        .describe("Batch parameter mutation request."),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("batch_set_parameters", params);
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(response, null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `batch_set_parameters failed: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}
