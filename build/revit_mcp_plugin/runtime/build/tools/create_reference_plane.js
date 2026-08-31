import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateReferencePlaneTool(server) {
    server.tool("create_reference_plane", "Create reference planes in the Revit model. Supports reference planes defined by start/end points, normal vector, and view. All units in mm.\n在 Revit 中创建参照平面。支持通过起点/终点、法线向量和视图定义参照平面。所有单位为毫米。", {
        data: z.array(z.object({
            startPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point of the reference plane in mm / 参照平面起点（毫米）"),
            endPoint: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point of the reference plane in mm / 参照平面终点（毫米）"),
            normal: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Normal vector of the reference plane / 参照平面法线向量"),
            viewName: z.string().optional().describe("View name where the reference plane is visible / 参照平面可见的视图名称"),
        })).describe("Array of reference planes to create / 要创建的参照平面数组"),
    }, async (args, extra) => {
        // Map TypeScript field names to C# model field names
        // TS sends startPoint/endPoint, C# expects bubbleEnd/freeEnd
        const params = {
            data: args.data.map((item) => ({
                bubbleEnd: item.startPoint,
                freeEnd: item.endPoint,
                normal: item.normal,
                viewName: item.viewName,
            })),
        };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_reference_plane", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create reference plane failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
