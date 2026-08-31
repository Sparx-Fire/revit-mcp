import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerPlaceScheduleOnSheetTool(server: McpServer) {
  server.tool(
    "place_schedule_on_sheet",
    "Place a schedule view onto a sheet in Revit. All coordinates in mm. / 在Revit中将明细表视图放置到图纸上。所有坐标单位为毫米。",
    {
      scheduleId: z.number().int().describe("Schedule view ID / 明细表视图ID"),
      sheetId: z.number().int().describe("Sheet ID to place on / 目标图纸ID"),
      location: z.object({
        x: z.number().describe("X position on sheet in mm / 图纸上的X位置(mm)"),
        y: z.number().describe("Y position on sheet in mm / 图纸上的Y位置(mm)"),
      }).describe("Location on sheet in mm / 图纸上的位置(mm)"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("place_schedule_on_sheet", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Place schedule on sheet failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
