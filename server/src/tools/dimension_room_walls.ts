import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerDimensionRoomWallsTool(server: McpServer) {
  server.tool(
    "dimension_room_walls",
    "Create chained wall dimensions for a room on the active floor plan. Uses room boundary walls, project DimensionType (by name or ID), and offset in millimeters.",
    {
      roomId: z
        .number()
        .int()
        .positive()
        .describe("Element ID of the room to dimension"),
      offsetMm: z
        .number()
        .positive()
        .optional()
        .default(300)
        .describe("Offset of dimension lines from the room boundary in mm"),
      dimensionType: z
        .string()
        .optional()
        .default("")
        .describe(
          "DimensionType name from the project (e.g. 'Linear'). Empty uses the first linear type."
        ),
      dimensionStyleId: z
        .number()
        .optional()
        .default(-1)
        .describe("DimensionType element ID. Used when name lookup fails."),
      viewId: z
        .number()
        .optional()
        .default(-1)
        .describe("Floor plan view ID. -1 uses the active view."),
    },
    async (args, extra) => {
      const params = {
        roomId: args.roomId,
        offsetMm: args.offsetMm,
        dimensionType: args.dimensionType,
        dimensionStyleId: args.dimensionStyleId,
        viewId: args.viewId,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("dimension_room_walls", params);
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
              text: `Room wall dimension creation failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
