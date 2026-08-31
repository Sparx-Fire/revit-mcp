import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerQueryViewRangeTool(server: McpServer) {
  server.tool(
    "query_view_range",
    "Get the view range of a plan view in Revit. Returns top, cut plane, bottom, and view depth levels with offsets. / 获取Revit平面视图的视图范围，返回顶部、剖切面、底部和视图深度的标高及偏移。",
    {
      viewId: z.number().int().describe("The plan view ID to query view range for / 要查询视图范围的平面视图ID"),
    },
    async (args, extra) => {
      const params = { viewId: args.viewId };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("query_view_range", params);
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
              text: `Query view range failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
