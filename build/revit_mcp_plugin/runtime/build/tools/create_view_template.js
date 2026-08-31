import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateViewTemplateTool(server) {
    server.tool("create_view_template", "Create a view template from an existing view in Revit. / 在Revit中从现有视图创建视图样板。", {
        sourceViewId: z.number().int().describe("Source view ID to create template from / 源视图ID"),
        name: z.string().describe("Template view name / 样板视图名称"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_view_template", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Create view template failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
