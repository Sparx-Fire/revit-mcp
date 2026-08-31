import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreatePipeTool(server: McpServer) {
  server.tool(
    "create_pipe",
    "Create pipes in the Revit model. Supports pipes with start/end points, diameter, system type, and level. All units in mm.\n在 Revit 中创建管道。支持设置起点/终点、直径、系统类型和标高。所有单位为毫米。",
    {
      data: z.array(z.object({
        startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point in mm / 起点坐标（毫米）"),
        endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point in mm / 终点坐标（毫米）"),
        diameter: z.number().describe("Pipe diameter in mm / 管道直径（毫米）"),
        baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
        baseOffset: z.number().optional().describe("Base offset in mm / 底部偏移（毫米）"),
        systemType: z.string().optional().describe("System type (Domestic Cold Water, Sanitary, etc.) / 系统类型"),
        pipeType: z.string().optional().describe("Pipe type name / 管道类型名称"),
        typeId: z.number().optional().describe("Pipe type ID / 管道类型 ID"),
      })).describe("Array of pipes to create / 要创建的管道数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_pipe", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create pipe failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
