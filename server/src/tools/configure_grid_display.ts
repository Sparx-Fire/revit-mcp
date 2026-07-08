import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerConfigureGridDisplayTool(server: McpServer) {
  server.tool(
    "configure_grid_display",
    "Configure 2D grid extents and bubble display on all floor plans for existing grids without recreating them. Applies GridType from the project by name or ID.",
    {
      gridIds: z
        .array(z.number().int().positive())
        .optional()
        .default([])
        .describe("Grid element IDs to update. Empty updates all grids."),
      gridTypeName: z
        .string()
        .optional()
        .default("")
        .describe("GridType name from the project"),
      gridTypeId: z
        .number()
        .optional()
        .default(-1)
        .describe("GridType element ID. Used when name lookup fails."),
      xExtentMin: z
        .number()
        .optional()
        .describe("Minimum X extent in mm for horizontal grids"),
      xExtentMax: z
        .number()
        .optional()
        .describe("Maximum X extent in mm for horizontal grids"),
      yExtentMin: z
        .number()
        .optional()
        .describe("Minimum Y extent in mm for vertical grids"),
      yExtentMax: z
        .number()
        .optional()
        .describe("Maximum Y extent in mm for vertical grids"),
      showBubbles: z
        .boolean()
        .optional()
        .default(true)
        .describe("Show bubbles at both grid ends on floor plans"),
      applyToAllFloorPlans: z
        .boolean()
        .optional()
        .default(true)
        .describe("Apply 2D extents to all non-template floor plans"),
    },
    async (args) => {
      const params = {
        gridIds: args.gridIds,
        gridTypeName: args.gridTypeName,
        gridTypeId: args.gridTypeId,
        xExtentMin: args.xExtentMin,
        xExtentMax: args.xExtentMax,
        yExtentMin: args.yExtentMin,
        yExtentMax: args.yExtentMax,
        showBubbles: args.showBubbles,
        applyToAllFloorPlans: args.applyToAllFloorPlans,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("configure_grid_display", params);
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
              text: `Configure grid display failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
