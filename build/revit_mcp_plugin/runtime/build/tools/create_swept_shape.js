import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateSweptShapeTool(server) {
    server.tool("create_swept_shape", "Create swept solid shapes along a path with configurable section profiles (rect, circle, horseshoe). All units in mm.\n沿路径创建扫掠实体，支持可配置截面轮廓（矩形、圆形、马蹄形）。所有单位为毫米。", {
        data: z.array(z.object({
            sectionType: z.enum(["Rect", "Circle", "Horseshoe"]).describe("Section profile type / 截面轮廓类型"),
            width: z.number().optional().describe("Section width in mm (Rect/Horseshoe) / 截面宽度（毫米）"),
            height: z.number().optional().describe("Section height in mm (Rect/Horseshoe) / 截面高度（毫米）"),
            radius: z.number().optional().describe("Section radius in mm (Circle) / 截面半径（毫米）"),
            pathPoints: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).describe("Sweep path points in mm / 扫掠路径点（毫米）"),
            category: z.string().optional().describe("Target category name / 目标类别名称"),
        })).describe("Array of swept shapes to create / 要创建的扫掠形状数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_swept_shape", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create swept shape failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
