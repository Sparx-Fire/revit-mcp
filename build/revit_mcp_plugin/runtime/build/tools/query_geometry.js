import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerQueryGeometryTool(server) {
    server.tool("query_geometry", "Query geometry information of a Revit element. Returns bounding box, solid count, face details. / 查询Revit图元的几何信息，返回边界框、实体数量和面详情。", {
        elementId: z.number().int().describe("The element ID to query geometry for / 要查询几何信息的图元ID"),
        viewId: z.number().int().optional().describe("Optional view ID for geometry computation / 可选的视图ID（用于几何计算）"),
        detailLevel: z.number().int().optional().describe("Optional detail level (0=Coarse, 1=Medium, 2=Fine) / 可选的细节层次"),
    }, async (args, extra) => {
        const params = { elementId: args.elementId };
        if (args.viewId !== undefined)
            params.viewId = args.viewId;
        if (args.detailLevel !== undefined)
            params.detailLevel = args.detailLevel;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("query_geometry", params);
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
                        text: `Query geometry failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
