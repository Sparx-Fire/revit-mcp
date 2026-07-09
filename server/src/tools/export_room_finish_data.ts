import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerExportRoomFinishDataTool(server: McpServer) {
  server.tool(
    "export_room_finish_data",
    "Export room finish data from the current Revit project (read-only). For each room returns roomId, floorFinish, wallFinish, ceilingFinish, boundary face materials with areas in m², and warnings when finish parameters are empty. Foundation for create_finish_schedule workflows.",
    {
      includeUnplacedRooms: z
        .boolean()
        .optional()
        .default(false)
        .describe(
          "Whether to include unplaced rooms (rooms not yet placed in the model). Defaults to false."
        ),
      includeNotEnclosedRooms: z
        .boolean()
        .optional()
        .default(false)
        .describe(
          "Whether to include rooms that are not fully enclosed. Defaults to false."
        ),
    },
    async (args) => {
      const params = {
        includeUnplacedRooms: args.includeUnplacedRooms ?? false,
        includeNotEnclosedRooms: args.includeNotEnclosedRooms ?? false,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("export_room_finish_data", params);
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
              text: `Export room finish data failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
