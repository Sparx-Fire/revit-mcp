import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateRoofTool(server: McpServer) {
  server.tool(
    "create_roof",
    "Create roofs in the Revit model. Supports flat, gable, and hip roofs with level, thickness, slope, overhang, and material. All units in mm.\n在 Revit 中创建屋顶。支持平屋顶、双坡屋顶和四坡屋顶，可设置标高、厚度、坡度、挑檐和材质。所有单位为毫米。",
    {
      data: z.array(z.object({
        type: z.string().describe("Roof type: Flat, Gable, Hip / 屋顶类型"),
        level: z.number().describe("Roof elevation in mm / 屋顶标高（毫米）"),
        height: z.number().optional().describe("Roof height for pitched roofs in mm / 坡屋顶高度（毫米）"),
        thickness: z.number().optional().describe("Roof thickness in mm / 屋顶厚度（毫米）"),
        slope: z.number().optional().describe("Roof slope in degrees / 屋顶坡度（度）"),
        overhang: z.number().optional().describe("Roof overhang from walls in mm / 屋顶挑檐（毫米）"),
        material: z.string().optional().describe("Roof material / 屋顶材质"),
      })).describe("Array of roofs to create / 要创建的屋顶数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_roof", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create roof failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
