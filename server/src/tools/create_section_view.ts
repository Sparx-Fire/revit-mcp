import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateSectionViewTool(server: McpServer) {
  server.tool(
    "create_section_view",
    "Create a section view in Revit with bounding box and view family type. All units in mm. / 在Revit中创建带有边界框和视图族类型的剖面视图。所有单位为毫米。",
    {
      name: z.string().optional().describe("View name / 视图名称"),
      boundingBox: z.object({
        minX: z.number().optional().default(-50000).describe("Min X in mm / 最小X(mm)"),
        minY: z.number().optional().default(-50000).describe("Min Y in mm / 最小Y(mm)"),
        minZ: z.number().optional().default(-50000).describe("Min Z in mm / 最小Z(mm)"),
        maxX: z.number().optional().default(50000).describe("Max X in mm / 最大X(mm)"),
        maxY: z.number().optional().default(50000).describe("Max Y in mm / 最大Y(mm)"),
        maxZ: z.number().optional().default(50000).describe("Max Z in mm / 最大Z(mm)"),
      }).optional().describe("Section bounding box / 剖面边界框"),
      viewFamilyTypeName: z.string().optional().default("Section").describe("View family type name / 视图族类型名称"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_section_view", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create section view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
