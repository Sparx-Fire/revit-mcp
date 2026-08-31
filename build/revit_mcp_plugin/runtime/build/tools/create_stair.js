import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateStairTool(server) {
    server.tool("create_stair", "Create stairs in the Revit model. Supports stairs with location, direction, levels, width, riser/tread parameters, landing, and type. All units in mm.\n在 Revit 中创建楼梯。支持设置位置、方向、标高、宽度、踏步参数、平台和类型。所有单位为毫米。", {
        data: z.array(z.object({
            location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Stair start location in mm / 楼梯起点位置（毫米）"),
            direction: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Stair direction vector / 楼梯方向向量"),
            baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
            topLevel: z.number().describe("Top level elevation in mm / 顶部标高（毫米）"),
            width: z.number().describe("Stair width in mm / 楼梯宽度（毫米）"),
            riserHeight: z.number().optional().describe("Riser height in mm / 踏步高度（毫米）"),
            treadDepth: z.number().optional().describe("Tread depth in mm / 踏板深度（毫米）"),
            stepCount: z.number().optional().describe("Number of steps / 踏步数量"),
            typeId: z.number().optional().describe("Stair type ID / 楼梯类型 ID"),
            stairType: z.string().optional().describe("Stair type name / 楼梯类型名称"),
            material: z.string().optional().describe("Stair material / 楼梯材质"),
            hasLanding: z.boolean().optional().describe("Whether the stair has a landing / 是否有平台"),
            landingWidth: z.number().optional().describe("Landing width in mm / 平台宽度（毫米）"),
            landingDepth: z.number().optional().describe("Landing depth in mm / 平台深度（毫米）"),
        })).describe("Array of stairs to create / 要创建的楼梯数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_stair", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create stair failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
