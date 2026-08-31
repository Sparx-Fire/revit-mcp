import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateEquipmentTool(server: McpServer) {
  server.tool(
    "create_equipment",
    "Create MEP equipment instances in the Revit model. Supports placement of mechanical, electrical, and plumbing equipment with location, rotation, and family type. All units in mm.\n在 Revit 中创建 MEP 设备实例。支持放置机械、电气和管道设备，可设置位置、旋转角度和族类型。所有单位为毫米。",
    {
      data: z.array(z.object({
        location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Equipment location in mm / 设备位置（毫米）"),
        rotation: z.number().optional().describe("Rotation around Z-axis in degrees / 绕 Z 轴旋转角度（度）"),
        baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
        baseOffset: z.number().optional().describe("Base offset in mm / 底部偏移（毫米）"),
        category: z.string().optional().describe("Equipment category (Mechanical Equipment, Electrical Equipment, etc.) / 设备类别"),
        equipmentType: z.string().optional().describe("Equipment type name / 设备类型名称"),
        familyName: z.string().optional().describe("Family name / 族名称"),
        typeId: z.number().optional().describe("Family type ID / 族类型 ID"),
      })).describe("Array of equipment to create / 要创建设备数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_equipment", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create equipment failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
