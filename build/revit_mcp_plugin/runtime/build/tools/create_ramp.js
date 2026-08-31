import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateRampTool(server) {
    server.tool("create_ramp", "Create ramps in the Revit model. Supports ramps with location, width, levels, type, and material. All units in mm.\n在 Revit 中创建坡道。支持设置位置、宽度、标高、类型和材质。所有单位为毫米。", {
        data: z.array(z.object({
            startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Ramp start point in mm / 坡道起点（毫米）"),
            endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Ramp end point in mm / 坡道终点（毫米）"),
            width: z.number().describe("Ramp width in mm / 坡道宽度（毫米）"),
            baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
            topLevel: z.number().describe("Top level elevation in mm / 顶部标高（毫米）"),
            baseOffset: z.number().optional().describe("Base offset in mm / 底部偏移（毫米）"),
            topOffset: z.number().optional().describe("Top offset in mm / 顶部偏移（毫米）"),
            typeId: z.number().optional().describe("Ramp type ID / 坡道类型 ID"),
            rampType: z.string().optional().describe("Ramp type name / 坡道类型名称"),
            material: z.string().optional().describe("Ramp material / 坡道材质"),
            slope: z.number().optional().describe("Ramp slope in percent / 坡道坡度（百分比）"),
        })).describe("Array of ramps to create / 要创建的坡道数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_ramp", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create ramp failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
