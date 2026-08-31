import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerExportRoomDataTool(server) {
    server.tool("export_room_data", "Export all room data from the current Revit project. Returns detailed information about each room including name, number, level, area, volume, perimeter, department, and more. Useful for generating room schedules, space analysis, and facility management data.", {
        includeUnplacedRooms: z
            .boolean()
            .optional()
            .default(false)
            .describe("Whether to include unplaced rooms (rooms not yet placed in the model). Defaults to false."),
        includeNotEnclosedRooms: z
            .boolean()
            .optional()
            .default(false)
            .describe("Whether to include rooms that are not fully enclosed. Defaults to false."),
    }, async (args, extra) => {
        const params = {
            includeUnplacedRooms: args.includeUnplacedRooms ?? false,
            includeNotEnclosedRooms: args.includeNotEnclosedRooms ?? false,
        };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("export_room_data", params);
            });
            return {
                content: [
                    {
                        type: "text",
                        text: JSON.stringify(response, null, 2),
                    },
                ],
            };
        }
        catch (error) {
            return {
                content: [
                    {
                        type: "text",
                        text: `Export room data failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
