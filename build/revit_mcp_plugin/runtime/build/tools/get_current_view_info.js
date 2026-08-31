import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerGetCurrentViewInfoTool(server) {
    server.tool("get_current_view_info", "获取 Revit 当前活动视图的详细信息，包括视图类型、名称、比例等属性。", {}, async (args, extra) => {
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("get_current_view_info", {});
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
                        text: `get current view info failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
