import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateOpeningTool(server) {
    server.tool("create_opening", "Create openings in the Revit model. Supports wall openings, floor openings, roof openings, and shaft openings with host element, location, and dimensions. All units in mm.\n在 Revit 中创建洞口。支持墙洞口、楼板洞口、屋顶洞口和竖井洞口，可设置宿主图元、位置和尺寸。所有单位为毫米。", {
        data: z.array(z.object({
            hostElementId: z.number().describe("Host element ID / 宿主图元 ID"),
            openingType: z.string().optional().describe("Opening type: Wall, Floor, Roof, Shaft / 洞口类型"),
            location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Opening location in mm / 洞口位置（毫米）"),
            width: z.number().describe("Opening width in mm / 洞口宽度（毫米）"),
            height: z.number().describe("Opening height in mm / 洞口高度（毫米）"),
            baseLevel: z.number().optional().describe("Base level elevation in mm / 基准标高（毫米）"),
            topLevel: z.number().optional().describe("Top level elevation in mm / 顶部标高（毫米）"),
        })).describe("Array of openings to create / 要创建的洞口数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_opening", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create opening failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
