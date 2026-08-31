import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateFloorTool(server) {
    server.tool("create_floor", "Create floors in the Revit model. Create floors with boundary points, thickness, level, and type. All units in mm.\n在 Revit 中创建楼板。支持通过边界点、厚度、标高和类型创建楼板。所有单位为毫米。", {
        data: z.array(z.object({
            level: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
            thickness: z.number().optional().describe("Floor thickness in mm / 楼板厚度（毫米）"),
            height: z.number().optional().describe("Floor height in mm / 楼板高度（毫米）"),
            boundaryPoints: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).describe("Floor boundary points in mm / 楼板边界点坐标（毫米）"),
            levelOffset: z.number().optional().describe("Level offset in mm / 标高偏移（毫米）"),
            typeId: z.number().optional().describe("Floor type ID / 楼板类型 ID"),
            floorType: z.string().optional().describe("Floor type name / 楼板类型名称"),
            material: z.string().optional().describe("Floor material / 楼板材质"),
            isStructural: z.boolean().optional().describe("Whether the floor is structural / 是否为结构楼板"),
            levelName: z.string().optional().describe("Level name / 标高名称"),
        })).describe("Array of floors to create / 要创建的楼板数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_floor", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create floor failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
