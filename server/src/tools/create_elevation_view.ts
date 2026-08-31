import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateElevationViewTool(server: McpServer) {
  server.tool(
    "create_elevation_view",
    "Create an elevation view in Revit using an elevation marker. Direction index 0-3 maps to project north/south/east/west. / 在Revit中使用立面标记创建立面视图。方向索引0-3对应项目北/南/东/西。",
    {
      name: z.string().optional().describe("View name / 视图名称"),
      directionIndex: z.number().int().min(0).max(3).optional().default(0).describe("Direction index (0-3) / 方向索引"),
      viewFamilyTypeName: z.string().optional().default("Elevation").describe("View family type name / 视图族类型名称"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_elevation_view", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create elevation view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
