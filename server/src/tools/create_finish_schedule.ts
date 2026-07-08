import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const finishScheduleSchema = z.object({
  name: z
    .string()
    .optional()
    .default("Room Finish Schedule")
    .describe("Schedule view name"),
  templateId: z
    .string()
    .optional()
    .default("")
    .describe("UniqueId or ElementId of an existing room finish schedule template in the project"),
  type: z
    .enum(["Regular", "KeySchedule", "MaterialTakeoff"])
    .optional()
    .default("Regular"),
  includeUnplacedRooms: z.boolean().optional().default(false),
  includeNotEnclosedRooms: z.boolean().optional().default(false),
  missingFinishWarningThreshold: z
    .number()
    .optional()
    .default(0.3)
    .describe("Warn when share of rooms without finish data exceeds this ratio (0.3 = 30%)"),
});

export function registerCreateFinishScheduleTool(server: McpServer) {
  server.tool(
    "create_finish_schedule",
    "Create a room finish ViewSchedule from export_room_finish_data validation and project template. Rows are rooms; columns are floor/wall/ceiling finish types. Warns when more than 30% of rooms lack finish parameters.",
    {
      schedule: finishScheduleSchema
        .optional()
        .describe("Finish schedule settings"),
      includeUnplacedRooms: z
        .boolean()
        .optional()
        .describe("Include unplaced rooms in finish data validation"),
      includeNotEnclosedRooms: z
        .boolean()
        .optional()
        .describe("Include not enclosed rooms in finish data validation"),
    },
    async (args) => {
      const schedule = {
        ...finishScheduleSchema.parse({}),
        ...args.schedule,
        includeUnplacedRooms:
          args.schedule?.includeUnplacedRooms ?? args.includeUnplacedRooms ?? false,
        includeNotEnclosedRooms:
          args.schedule?.includeNotEnclosedRooms ?? args.includeNotEnclosedRooms ?? false,
      };

      const params = { schedule };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_finish_schedule", params);
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
              text: `Create finish schedule failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
