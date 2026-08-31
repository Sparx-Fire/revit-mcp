import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateDetailCurveTool(server) {
    server.tool("create_detail_curve", "Create detail lines in a Revit view. All coordinates in mm. / 在Revit视图中创建详图线。所有坐标单位为毫米。", {
        viewId: z.number().int().describe("Target view ID / 目标视图ID"),
        lines: z.array(z.object({
            startX: z.number().describe("Start X in mm / 起点X(mm)"),
            startY: z.number().describe("Start Y in mm / 起点Y(mm)"),
            endX: z.number().describe("End X in mm / 终点X(mm)"),
            endY: z.number().describe("End Y in mm / 终点Y(mm)"),
        })).describe("Array of lines to create / 要创建的线条数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_detail_curve", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create detail curve failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
