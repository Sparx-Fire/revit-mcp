import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateDuctTool(server: McpServer) {
  server.tool(
    "create_duct",
    "Create ducts in the Revit model. Supports rectangular and round ducts with start/end points, width, height, system type, and level. All units in mm.\n在 Revit 中创建风管。支持矩形和圆形风管，可设置起点/终点、宽度、高度、系统类型和标高。所有单位为毫米。",
    {
      data: z.array(z.object({
        startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point in mm / 起点坐标（毫米）"),
        endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point in mm / 终点坐标（毫米）"),
        width: z.number().describe("Duct width/diameter in mm / 风管宽度/直径（毫米）"),
        height: z.number().optional().describe("Duct height for rectangular in mm / 矩形风管高度（毫米）"),
        baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
        baseOffset: z.number().optional().describe("Base offset in mm / 底部偏移（毫米）"),
        systemType: z.string().optional().describe("System type (Supply Air, Return Air, Exhaust Air) / 系统类型"),
        ductType: z.string().optional().describe("Duct type name / 风管类型名称"),
        typeId: z.number().optional().describe("Duct type ID / 风管类型 ID"),
      })).describe("Array of ducts to create / 要创建的风管数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_duct", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create duct failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
