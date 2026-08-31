import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateColumnTool(server) {
    server.tool("create_column", "Create columns in the Revit model. Supports structural columns with location, dimensions, levels, type, and material. All units in mm.\n在 Revit 中创建柱。支持结构柱，可设置位置、尺寸、标高、类型和材质。所有单位为毫米。", {
        data: z.array(z.object({
            location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Column location in mm / 柱位置坐标（毫米）"),
            height: z.number().describe("Column height in mm / 柱高度（毫米）"),
            width: z.number().optional().describe("Column width in mm / 柱宽度（毫米）"),
            depth: z.number().optional().describe("Column depth in mm / 柱深度（毫米）"),
            baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
            topLevel: z.number().optional().describe("Top level elevation in mm / 顶部标高（毫米）"),
            typeId: z.number().optional().describe("Column type ID / 柱类型 ID"),
            columnType: z.string().optional().describe("Column type name / 柱类型名称"),
            material: z.string().optional().describe("Column material / 柱材质"),
            isStructural: z.boolean().optional().describe("Whether the column is structural / 是否为结构柱"),
        })).describe("Array of columns to create / 要创建的柱数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_column", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create column failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
