import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateTagTool(server: McpServer) {
  server.tool(
    "create_tag",
    "Create tag annotations on elements in the current Revit view. Supports tagging doors, windows, walls, rooms, and other elements with configurable tag type, orientation, leader, and location. All coordinates are in millimeters (mm). / 在当前Revit视图中为图元创建标记。支持标记门、窗、墙、房间和其他图元，可配置标记类型、方向、引线和位置。所有坐标单位为毫米（mm）。",
    {
      data: z
        .array(
          z.object({
            elementId: z
              .number()
              .describe("Element ID of the element to tag / 要标记的图元ID"),
            location: z
              .object({
                x: z.number().describe("X coordinate in mm / X坐标（mm）"),
                y: z.number().describe("Y coordinate in mm / Y坐标（mm）"),
                z: z.number().describe("Z coordinate in mm / Z坐标（mm）"),
              })
              .describe("Tag placement location in mm / 标记放置位置（mm）"),
            orientation: z
              .number()
              .optional()
              .default(0)
              .describe("Tag orientation (0=Horizontal, 1=Vertical) / 标记方向（0=水平，1=垂直）"),
            hasLeader: z
              .boolean()
              .optional()
              .default(false)
              .describe("Whether the tag has a leader line / 标记是否带有引线"),
            tagTypeId: z
              .number()
              .optional()
              .default(-1)
              .describe("Element ID of the tag type. -1 for default / 标记类型元素ID，-1表示默认"),
            tagCategory: z
              .string()
              .optional()
              .default("")
              .describe("Tag category (Door, Window, Wall, Room, Multi) / 标记类别（Door, Window, Wall, Room, Multi）"),
            viewId: z
              .number()
              .optional()
              .default(-1)
              .describe("Element ID of the view. -1 for active view / 视图元素ID，-1表示当前视图"),
          })
        )
        .describe("Array of tags to create / 要创建的标记数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_tag", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Tag creation failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
