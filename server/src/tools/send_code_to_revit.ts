import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const transactionModeSchema = z
  .enum(["auto", "none"])
  .default("auto")
  .describe(
    "How the snippet should interact with Revit transactions. Use 'auto' to wrap the snippet in a transaction, or 'none' when the called code manages its own transactions."
  );

export function registerSendCodeToRevitTool(server: McpServer) {
  server.tool(
    "send_code_to_revit",
    `Sends a C# snippet for execution inside the live Revit session.

WHEN TO USE: Revit API tasks not covered by a dedicated tool (view templates, V/G overrides, filters, link visibility, bulk parameter edits).
WHEN NOT TO USE: If a dedicated tool exists (get_current_view_info, create_level, tag_all_walls, ...) prefer it - structured tools validate input.

ENVIRONMENT RULES (violations fail immediately):
- Document variable: 'document' (lowercase). 'doc', 'Document', 'uiapp' do not exist.
- The snippet runs inside a pre-built Execute method. You cannot add 'using' directives. Available namespaces: System, System.Linq, System.Collections.Generic, Autodesk.Revit.DB, Autodesk.Revit.UI. Fully qualify everything else (e.g. System.Text.StringBuilder).
- Transactions: transactionMode 'auto' (default) wraps your code in one transaction - do NOT create your own ('Starting a new transaction is not permitted'). Use transactionMode 'none' only when you must manage transactions yourself.
- Always end with 'return <string>;'. Use System.Text.StringBuilder for multi-line output.
- No multiple declarators: 'var a = x, b = y;' does not compile. Use separate lines.
- Revit 2022+ API: use SurfaceForegroundPatternColor / CutForegroundPatternColor (ProjectionFillColor / CutFillColor no longer exist).
- Categories are not Elements: 'GetElement(id) as Category' is always null - iterate document.Settings.Categories. Linked DWG categories appear there top-level.
- View templates cannot be Duplicated - use ElementTransformUtils.CopyElements.
- Check IsFilterApplied before GetFilterVisibility (crashes otherwise).

QUERY STRATEGY:
- ~2 minute execution timeout. Split heavy work: PROBE -> READ -> APPLY -> VERIFY, and get user approval between READ and APPLY for model-modifying changes.

RELATED TOOLS: get_current_view_info (probe the active view first), get_selected_elements (operate on the user's selection), query_stored_data.`,
    {
      code: z
        .string()
        .describe(
          "The C# snippet body inserted into the Execute method. It has access to 'document' (the active Revit Document) and 'parameters' (object[]). Must end with a return statement producing a string."
        ),
      parameters: z
        .array(z.string())
        .optional()
        .describe(
          "Optional string parameters passed to your code as the 'parameters' array"
        ),
      transactionMode: transactionModeSchema,
    },
    async (args, extra) => {
      const params = {
        code: args.code,
        parameters: args.parameters || [],
        transactionMode: args.transactionMode,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("send_code_to_revit", params);
        });

        return {
          content: [
            {
              type: "text",
              text: `Code execution successful!\nResult: ${JSON.stringify(
                response,
                null,
                2
              )}`,
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Code execution failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
