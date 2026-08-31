import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateRevisionCloudTool(server: McpServer) {
  server.tool(
    "create_revision_cloud",
    "Create a revision cloud in a Revit view with boundary points. All coordinates in mm. / 在Revit视图中创建带有边界点的修订云线。所有坐标单位为毫米。",
    {
      revisionId: z.number().int().describe("Revision ID to associate with / 关联的修订ID"),
      viewId: z.number().int().describe("View ID to place cloud in / 放置云线的视图ID"),
      points: z.array(z.object({
        x: z.number().describe("X coordinate in mm / X坐标(mm)"),
        y: z.number().describe("Y coordinate in mm / Y坐标(mm)"),
      })).describe("Boundary points in mm / 边界点（毫米）"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_revision_cloud", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create revision cloud failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
