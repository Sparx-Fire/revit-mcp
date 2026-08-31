import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateFilledRegionTool(server: McpServer) {
  server.tool(
    "create_filled_region",
    "Create a filled region in a Revit view with boundary points. All coordinates in mm. / 在Revit视图中创建带有边界点的填充区域。所有坐标单位为毫米。",
    {
      viewId: z.number().int().describe("Target view ID / 目标视图ID"),
      boundary: z.array(z.array(z.object({
        x: z.number().describe("X coordinate in mm / X坐标(mm)"),
        y: z.number().describe("Y coordinate in mm / Y坐标(mm)"),
      }))).describe("Boundary loops (array of point arrays) / 边界环（点数组的数组）"),
      filledRegionTypeName: z.string().optional().describe("Filled region type name / 填充区域类型名称"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_filled_region", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create filled region failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
