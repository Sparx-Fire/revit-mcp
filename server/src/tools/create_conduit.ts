import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateConduitTool(server: McpServer) {
  server.tool(
    "create_conduit",
    "Create conduits in the Revit model. Supports conduits with start/end points, diameter, and level. All units in mm.\n在 Revit 中创建线管。支持设置起点/终点、直径和标高。所有单位为毫米。",
    {
      data: z.array(z.object({
        startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point in mm / 起点坐标（毫米）"),
        endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point in mm / 终点坐标（毫米）"),
        diameter: z.number().describe("Conduit diameter in mm / 线管直径（毫米）"),
        baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
        baseOffset: z.number().optional().describe("Base offset in mm / 底部偏移（毫米）"),
        conduitType: z.string().optional().describe("Conduit type name / 线管类型名称"),
        typeId: z.number().optional().describe("Conduit type ID / 线管类型 ID"),
      })).describe("Array of conduits to create / 要创建的线管数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_conduit", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create conduit failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
