import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerSetViewPropertiesTool(server) {
    server.tool("set_view_properties", "Set view properties in Revit including scale, detail level, crop box, display style, and view template. All units in mm. / 在Revit中设置视图属性，包括比例、详细程度、裁剪框、显示样式和视图样板。所有单位为毫米。", {
        viewId: z.number().int().describe("View ID to modify / 要修改的视图ID"),
        properties: z.object({
            scale: z.number().int().optional().describe("View scale / 视图比例"),
            detailLevel: z.enum(["Coarse", "Medium", "Fine"]).optional().describe("Detail level / 详细程度"),
            displayStyle: z.enum(["wireframe", "hidden", "shaded", "consistent_colors", "realistic"]).optional().describe("Display style / 显示样式"),
            cropBox: z.object({
                minX: z.number(),
                minY: z.number(),
                maxX: z.number(),
                maxY: z.number(),
            }).optional().describe("Crop box in mm / 裁剪框(mm)"),
            templateId: z.number().int().optional().describe("View template ID to apply / 要应用的视图样板ID"),
        }).describe("View properties to set / 要设置的视图属性"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("set_view_properties", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Set view properties failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
