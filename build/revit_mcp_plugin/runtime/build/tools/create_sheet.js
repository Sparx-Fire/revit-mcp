import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateSheetTool(server) {
    server.tool("create_sheet", "Create one or more sheets in Revit with optional title blocks. Supports sheet numbering, naming, title block selection by ID or family name, and revision assignment. / 在Revit中创建一个或多个图纸，可选标题栏。支持图纸编号、命名、通过ID或族名称选择标题栏，以及修订分配。", {
        data: z
            .array(z.object({
            sheetNumber: z
                .string()
                .optional()
                .describe("Sheet number (e.g., A101) / 图纸编号"),
            sheetName: z
                .string()
                .optional()
                .describe("Sheet name / 图纸名称"),
            titleBlockTypeId: z
                .number()
                .optional()
                .describe("Title block type ID / 标题栏类型ID"),
            titleBlockFamilyName: z
                .string()
                .optional()
                .describe("Title block family name / 标题栏族名称"),
            titleBlockTypeName: z
                .string()
                .optional()
                .describe("Title block type name / 标题栏类型名称"),
            revisionIds: z
                .array(z.number())
                .optional()
                .describe("Revision IDs to apply / 要应用的修订ID列表"),
            parameters: z
                .record(z.any())
                .optional()
                .describe("Additional sheet parameters / 附加图纸参数"),
        }))
            .describe("Array of sheets to create / 要创建的图纸数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_sheet", params);
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
                        text: `Create sheet failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
