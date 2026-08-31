import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateDirectShapeTool(server) {
    server.tool("create_direct_shape", "Create primitive solid geometry shapes (box, cylinder, extrusion) as DirectShape elements in Revit. All units in mm.\n在 Revit 中创建基本几何实体（长方体、圆柱体、拉伸体）作为 DirectShape 图元。所有单位为毫米。", {
        data: z.array(z.object({
            shapeType: z.enum(["Box", "Cylinder", "Extrusion"]).describe("Shape type / 形状类型"),
            width: z.number().optional().describe("Width in mm (Box) / 宽度（毫米）"),
            depth: z.number().optional().describe("Depth in mm (Box) / 深度（毫米）"),
            height: z.number().optional().describe("Height in mm (Box/Cylinder/Extrusion) / 高度（毫米）"),
            radius: z.number().optional().describe("Radius in mm (Cylinder) / 半径（毫米）"),
            center: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Center point in mm / 中心点坐标（毫米）"),
            curveType: z.string().optional().describe("Curve type for extrusion profile (Line) / 拉伸轮廓曲线类型"),
            points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).optional().describe("Profile points for extrusion in mm / 拉伸轮廓点（毫米）"),
            extrusionDir: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Extrusion direction vector / 拉伸方向向量"),
            extrusionLength: z.number().optional().describe("Extrusion length in mm / 拉伸长度（毫米）"),
            category: z.string().optional().describe("Target category name / 目标类别名称"),
            material: z.string().optional().describe("Material name / 材质名称"),
            typeId: z.number().optional().describe("Type ID / 类型 ID"),
        })).describe("Array of shapes to create / 要创建的形状数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_direct_shape", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create direct shape failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
