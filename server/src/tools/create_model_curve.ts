import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateModelCurveTool(server: McpServer) {
  server.tool(
    "create_model_curve",
    "Create model curves in the Revit model. Supports lines and arcs with start/end points, curve type, and sketch plane level. All units in mm.\n在 Revit 中创建模型线。支持直线和弧线，可设置起点/终点、曲线类型和草图平面标高。所有单位为毫米。",
    {
      data: z.array(z.object({
        curveType: z.string().default("Line").describe("Curve type: Line, Arc, Circle, Spline / 曲线类型"),
        points: z.array(z.object({
          x: z.number(),
          y: z.number(),
          z: z.number()
        })).describe("Points defining the curve in mm. For Line: [start, end]; for Arc: [p1, p2, p3]; for Circle/Spline: at least 2 points / 定义曲线的点（毫米）。直线: [起点, 终点]; 弧线: [p1, p2, p3]; 圆/样条: 至少2个点"),
        center: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Center point for Circle in mm / 圆心坐标（毫米）（用于圆）"),
        radius: z.number().optional().describe("Radius for Circle in mm / 半径（毫米）（用于圆）"),
        sketchPlaneId: z.number().optional().describe("Sketch plane element ID (optional). If not provided, a plane will be created automatically / 草图平面图元ID（可选，不传则自动创建）"),
        lineStyle: z.string().optional().describe("Line style name / 线样式名称"),
      })).describe("Array of model curves to create / 要创建的模型线数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_model_curve", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create model curve failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
