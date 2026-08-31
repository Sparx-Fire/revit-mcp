import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCheckInterferencesTool(server: McpServer) {
  server.tool(
    "check_interferences",
    "Check interference/collision between Revit elements. Returns pairs of colliding elements. / 检测Revit图元之间的碰撞/干涉，返回碰撞图元对。",
    {
      elementIds: z.array(z.number().int().positive()).min(2).describe("Array of element IDs to check for interferences / 要检测干涉的图元ID数组"),
    },
    async (args, extra) => {
      const params = { elementIds: args.elementIds };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("check_interferences", params);
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
              text: `Check interferences failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
