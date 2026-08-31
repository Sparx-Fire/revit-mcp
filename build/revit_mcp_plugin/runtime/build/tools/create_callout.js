import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateCalloutTool(server) {
    server.tool("create_callout", "Create a callout view from a host view in Revit with bounding box. All units in mm. / 在Revit中从宿主视图创建带有边界框的详图索引视图。所有单位为毫米。", {
        name: z.string().optional().describe("Callout view name / 详图索引视图名称"),
        hostViewId: z.number().int().describe("Host view ID / 宿主视图ID"),
        boundingBox: z.object({
            minX: z.number().describe("Min X in mm / 最小X(mm)"),
            minY: z.number().describe("Min Y in mm / 最小Y(mm)"),
            maxX: z.number().describe("Max X in mm / 最大X(mm)"),
            maxY: z.number().describe("Max Y in mm / 最大Y(mm)"),
        }).describe("Callout bounding box / 详图索引边界框"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_callout", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create callout failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
