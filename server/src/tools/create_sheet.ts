import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const sheetSchema = z.object({
  sheetNumber: z.string().describe("Sheet number (e.g. A101)"),
  sheetName: z.string().describe("Sheet name"),
  titleBlockTypeId: z.number().optional().default(0),
  titleBlockFamilyName: z
    .string()
    .optional()
    .default("")
    .describe("Title block family name from the project"),
  titleBlockTypeName: z
    .string()
    .optional()
    .default("")
    .describe("Title block type name from the project"),
  revisionIds: z.array(z.number()).optional().default([]),
  parameters: z.record(z.unknown()).optional(),
});

export function registerCreateSheetTool(server: McpServer) {
  server.tool(
    "create_sheet",
    "Create a Revit sheet with a title block from the current project. Coordinates and view placement use place_view_on_sheet.",
    {
      sheet: sheetSchema.describe(
        "Sheet settings. Provide titleBlockFamilyName/titleBlockTypeName or titleBlockTypeId from project title blocks."
      ),
    },
    async (args) => {
      const params = { sheet: args.sheet };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_sheet", params);
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
              text: `Create sheet failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
