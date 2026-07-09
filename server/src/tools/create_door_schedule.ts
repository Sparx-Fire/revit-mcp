import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

function registerScheduleExportTool(
  server: McpServer,
  toolName: string,
  commandName: string,
  elementLabel: string
) {
  server.tool(
    toolName,
    `Export structured ${elementLabel} schedule data from the current Revit project. Returns rows grouped by family type with mark, type, size, level, and count. Foundation for create_schedule and validate_schedule workflows.`,
    {},
    async () => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(commandName, {});
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
              text: `Export ${elementLabel} schedule failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}

export function registerCreateDoorScheduleTool(server: McpServer) {
  registerScheduleExportTool(
    server,
    "create_door_schedule",
    "create_door_schedule",
    "door"
  );
}

export function registerCreateWindowScheduleTool(server: McpServer) {
  registerScheduleExportTool(
    server,
    "create_window_schedule",
    "create_window_schedule",
    "window"
  );
}

export function registerCreateFloorScheduleTool(server: McpServer) {
  registerScheduleExportTool(
    server,
    "create_floor_schedule",
    "create_floor_schedule",
    "floor"
  );
}
