import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerManageScheduleFieldsTool(server) {
    server.tool("manage_schedule_fields", "Manage fields in a Revit schedule: add, remove, reorder, hide, or show fields. / 管理Revit明细表中的字段：添加、移除、重新排序、隐藏或显示字段。", {
        scheduleId: z.number().int().describe("Schedule view ID / 明细表视图ID"),
        action: z.enum(["add", "remove", "reorder", "hide", "show"]).describe("Action to perform / 执行的操作"),
        fieldName: z.string().describe("Field name / 字段名称"),
        position: z.number().int().min(0).optional().describe("Position index (for add/reorder) / 位置索引（用于添加/重新排序）"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("manage_schedule_fields", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return { content: [{ type: "text", text: `Manage schedule fields failed: ${error instanceof Error ? error.message : String(error)}` }] };
        }
    });
}
