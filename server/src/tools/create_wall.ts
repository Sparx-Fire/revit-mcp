import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateWallTool(server: McpServer) {
  server.tool(
    "create_wall",
    "Create walls in the Revit model. Create walls with start/end points, height, thickness, level, and type. All units in mm.\n在 Revit 中创建墙体。支持通过起点/终点、高度、厚度、标高和类型创建墙体。所有单位为毫米。",
    {
      data: z.array(z.object({
        startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point of the wall in mm / 墙体起点坐标（毫米）"),
        endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point of the wall in mm / 墙体终点坐标（毫米）"),
        height: z.number().describe("Wall height in mm / 墙体高度（毫米）"),
        thickness: z.number().optional().describe("Wall thickness in mm / 墙体厚度（毫米）"),
        baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
        baseOffset: z.number().optional().describe("Base offset from level in mm / 底部偏移（毫米）"),
        topConstraintType: z.number().optional().describe("Top constraint type (0=Unconstrained, 1=Up to level, 2=Unconnected height) / 顶部约束类型"),
        topLevelId: z.number().optional().describe("Top level ID / 顶部标高 ID"),
        topOffset: z.number().optional().describe("Top offset in mm / 顶部偏移（毫米）"),
        wallType: z.string().optional().describe("Wall type name / 墙体类型名称"),
        typeId: z.number().optional().describe("Wall type ID / 墙体类型 ID"),
        material: z.string().optional().describe("Wall material / 墙体材质"),
        isStructural: z.boolean().optional().describe("Whether the wall is structural / 是否为结构墙"),
        flipped: z.boolean().optional().describe("Flip wall direction / 翻转墙体方向"),
      })).describe("Array of walls to create / 要创建的墙体数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_wall", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create wall failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
