import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerSetElementCurveTool(server) {
    server.tool("set_element_curve", "Modify the location curve of linear elements (walls, beams, pipes, ducts, etc.) by setting start and end points. / 修改线性图元（墙、梁、管道、风管等）的定位曲线，通过设置起点和终点。", {
        elementId: z.number().int().describe("The element ID to modify curve on / 要修改曲线的图元ID"),
        startPoint: z.object({
            x: z.number().describe("Start X coordinate / 起点X坐标"),
            y: z.number().describe("Start Y coordinate / 起点Y坐标"),
            z: z.number().describe("Start Z coordinate / 起点Z坐标"),
        }).describe("Start point of the curve in feet / 曲线起点（英尺）"),
        endPoint: z.object({
            x: z.number().describe("End X coordinate / 终点X坐标"),
            y: z.number().describe("End Y coordinate / 终点Y坐标"),
            z: z.number().describe("End Z coordinate / 终点Z坐标"),
        }).describe("End point of the curve in feet / 曲线终点（英尺）"),
    }, async (args, extra) => {
        const params = {
            elementId: args.elementId,
            startPoint: args.startPoint,
            endPoint: args.endPoint,
        };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("set_element_curve", params);
            });
            return {
                content: [
                    {
                        type: "text",
                        text: JSON.stringify(response, null, 2),
                    },
                ],
            };
        }
        catch (error) {
            return {
                content: [
                    {
                        type: "text",
                        text: `Set element curve failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
