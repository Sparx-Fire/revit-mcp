import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerCreateTextNoteTool(server) {
    server.tool("create_text_note", "Create text note annotations in the current Revit view. Supports multiple text notes with custom text content, location, rotation, width, alignment, and text note type. All coordinates are in millimeters (mm). / 在当前Revit视图中创建文字注释。支持多个文字注释，可自定义文本内容、位置、旋转角度、宽度、对齐方式和文字类型。所有坐标单位为毫米（mm）。", {
        data: z
            .array(z.object({
            location: z
                .object({
                x: z.number().describe("X coordinate in mm / X坐标（mm）"),
                y: z.number().describe("Y coordinate in mm / Y坐标（mm）"),
                z: z.number().describe("Z coordinate in mm / Z坐标（mm）"),
            })
                .describe("Text note location point in mm / 文字注释位置（mm）"),
            text: z
                .string()
                .describe("Text content of the note / 文字注释内容"),
            rotation: z
                .number()
                .optional()
                .default(0)
                .describe("Text rotation in degrees / 文字旋转角度（度）"),
            width: z
                .number()
                .optional()
                .default(0)
                .describe("Text width in mm (0 = no width limit) / 文字宽度mm（0=无宽度限制）"),
            textNoteTypeId: z
                .number()
                .optional()
                .default(-1)
                .describe("Element ID of the text note type. -1 for default / 文字类型元素ID，-1表示默认"),
            viewId: z
                .number()
                .optional()
                .default(-1)
                .describe("Element ID of the view. -1 for active view / 视图元素ID，-1表示当前视图"),
            horizontalAlign: z
                .number()
                .optional()
                .default(0)
                .describe("Horizontal alignment (0=Left, 1=Center, 2=Right) / 水平对齐（0=左，1=居中，2=右）"),
            verticalAlign: z
                .number()
                .optional()
                .default(0)
                .describe("Vertical alignment (0=Top, 1=Middle, 2=Bottom) / 垂直对齐（0=顶，1=居中，2=底）"),
        }))
            .describe("Array of text notes to create / 要创建的文字注释数组"),
    }, async (args, extra) => {
        const params = args;
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("create_text_note", params);
            });
            return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
        }
        catch (error) {
            return {
                content: [
                    {
                        type: "text",
                        text: `Text note creation failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
