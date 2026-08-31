import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerManageGraphicsResourcesTool(server) {
    server.tool("manage_graphics_resources", "Manage graphics resources in Revit: line styles and fill patterns. / 管理Revit中的图形资源：线样式和填充图案。", {
        action: z.enum(["line_style", "fill_pattern"]).describe("Resource type to manage / 要管理的资源类型"),
        name: z.string().describe("Resource name / 资源名称"),
        properties: z.object({
            color: z.object({
                r: z.number().int().min(0).max(255),
                g: z.number().int().min(0).max(255),
                b: z.number().int().min(0).max(255),
            }).optional().describe("RGB color / RGB颜色"),
            lineWeight: z.number().int().optional().describe("Line weight (for line_style) / 线重（用于线样式）"),
            linePattern: z.string().optional().describe("Line pattern name (for line_style) / 线样式名称（用于线样式）"),
        }).optional().describe("Resource properties / 资源属性"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("manage_graphics_resources", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Manage graphics resources failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
