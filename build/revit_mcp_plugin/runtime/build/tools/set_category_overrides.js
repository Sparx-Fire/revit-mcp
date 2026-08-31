import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerSetCategoryOverridesTool(server) {
    server.tool("set_category_overrides", "Set graphic overrides for a category in a specific view. / 在特定视图中设置类别的图形覆盖。", {
        viewId: z.number().int().describe("View ID / 视图ID"),
        categoryId: z.number().int().describe("Category ID to override / 要覆盖的类别ID"),
        overrides: z.object({
            color: z.object({
                r: z.number().int().min(0).max(255),
                g: z.number().int().min(0).max(255),
                b: z.number().int().min(0).max(255),
            }).optional().describe("RGB color / RGB颜色"),
            lineWeight: z.number().int().optional().describe("Line weight / 线宽"),
            fillPattern: z.string().optional().describe("Fill pattern name / 填充图案名称"),
            halftone: z.boolean().optional().describe("Apply halftone / 应用半色调"),
            transparency: z.number().int().min(0).max(100).optional().describe("Transparency (0-100) / 透明度(0-100)"),
        }).describe("Override settings / 覆盖设置"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("set_category_overrides", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Set category overrides failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
