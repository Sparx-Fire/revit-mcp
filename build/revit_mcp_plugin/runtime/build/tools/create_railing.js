import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateRailingTool(server) {
    server.tool("create_railing", "Create railings in the Revit model. Supports railings with start/end points, height, level, type, and material. All units in mm.\n在 Revit 中创建栏杆。支持通过起点/终点、高度、标高、类型和材质创建栏杆。所有单位为毫米。", {
        data: z.array(z.object({
            startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Railing start point in mm / 栏杆起点（毫米）"),
            endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Railing end point in mm / 栏杆终点（毫米）"),
            height: z.number().optional().describe("Railing height in mm / 栏杆高度（毫米）"),
            baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
            levelOffset: z.number().optional().describe("Level offset in mm / 标高偏移（毫米）"),
            typeId: z.number().optional().describe("Railing type ID / 栏杆类型 ID"),
            railingType: z.string().optional().describe("Railing type name / 栏杆类型名称"),
            material: z.string().optional().describe("Railing material / 栏杆材质"),
        })).describe("Array of railings to create / 要创建的栏杆数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_railing", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create railing failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
