import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateCeilingTool(server) {
    server.tool("create_ceiling", "Create ceilings in the Revit model. Supports ceilings with boundary points, thickness, level, and type. All units in mm.\n在 Revit 中创建天花板。支持通过边界点、厚度、标高和类型创建天花板。所有单位为毫米。", {
        data: z.array(z.object({
            boundaryPoints: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).describe("Ceiling boundary points in mm / 天花板边界点坐标（毫米）"),
            level: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
            thickness: z.number().optional().describe("Ceiling thickness in mm / 天花板厚度（毫米）"),
            levelOffset: z.number().optional().describe("Level offset in mm / 标高偏移（毫米）"),
            typeId: z.number().optional().describe("Ceiling type ID / 天花板类型 ID"),
            ceilingType: z.string().optional().describe("Ceiling type name / 天花板类型名称"),
            material: z.string().optional().describe("Ceiling material / 天花板材质"),
        })).describe("Array of ceilings to create / 要创建的天花板数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_ceiling", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create ceiling failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
