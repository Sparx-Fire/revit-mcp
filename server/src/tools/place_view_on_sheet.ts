import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const placementSchema = z.object({
  sheetId: z.number().optional().default(0),
  sheetUniqueId: z.string().optional().default(""),
  viewId: z.number().optional().default(0),
  viewUniqueId: z.string().optional().default(""),
  positionX: z
    .number()
    .describe("X position in millimeters from the lower-left sheet outline corner"),
  positionY: z
    .number()
    .describe("Y position in millimeters from the lower-left sheet outline corner"),
  viewportTypeId: z.number().optional().default(0),
  displayTitle: z.boolean().optional(),
  scaleOverride: z.number().optional().default(0),
  labelText: z.string().optional().default(""),
  rotation: z.number().optional().default(0),
  parameters: z.record(z.unknown()).optional(),
});

export function registerPlaceViewOnSheetTool(server: McpServer) {
  server.tool(
    "place_view_on_sheet",
    "Place a floor plan, view, or schedule on a sheet at coordinates in millimeters. Schedules use ScheduleSheetInstance; other views use Viewport.",
    {
      placement: placementSchema.describe(
        "Placement settings. Provide sheetId or sheetUniqueId and viewId or viewUniqueId. Positions are in mm from lower-left sheet outline; viewports are clamped inside the sheet."
      ),
    },
    async (args) => {
      const params = { placement: args.placement };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("place_view_on_sheet", params);
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
              text: `Place view on sheet failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
