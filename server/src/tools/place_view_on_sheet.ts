import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerPlaceViewOnSheetTool(server: McpServer) {
  server.tool(
    "place_view_on_sheet",
    "Place one or more views onto sheets in Revit as viewports. Supports position control in mm, viewport type, title display, scale override, and rotation. / 在Revit中将一个或多个视图作为视口放置到图纸上。支持毫米单位的定位、视口类型、标题显示、比例覆盖和旋转。",
    {
      data: z
        .array(
          z.object({
            sheetId: z
              .number()
              .describe("Sheet ID to place viewport on / 目标图纸ID"),
            viewId: z
              .number()
              .describe("View ID to place in viewport / 要放置的视图ID"),
            positionX: z
              .number()
              .describe("X position on sheet in mm / 图纸上的X位置(mm)"),
            positionY: z
              .number()
              .describe("Y position on sheet in mm / 图纸上的Y位置(mm)"),
            viewportTypeId: z
              .number()
              .optional()
              .describe("Viewport type ID / 视口类型ID"),
            displayTitle: z
              .boolean()
              .optional()
              .describe("Whether to display the view title / 是否显示视图标题"),
            scaleOverride: z
              .number()
              .optional()
              .describe("Override scale for the viewport / 视口比例覆盖"),
            labelText: z
              .string()
              .optional()
              .describe("Viewport label text / 视口标签文字"),
            rotation: z
              .number()
              .optional()
              .describe("Rotation angle in degrees / 旋转角度(度)"),
            parameters: z
              .record(z.any())
              .optional()
              .describe("Additional viewport parameters / 附加视口参数"),
          })
        )
        .describe("Array of viewports to place / 要放置的视口数组"),
    },
    async (args, extra) => {
      const params = args;

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
