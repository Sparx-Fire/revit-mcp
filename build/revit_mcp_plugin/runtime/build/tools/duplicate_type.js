import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerDuplicateTypeTool(server) {
    server.tool("duplicate_type", "Duplicate an element type in Revit and assign a new name. Returns the new type element ID. / 复制Revit中的图元类型并赋予新名称。返回新类型的图元ID。", {
        typeId: z.number().int().describe("The element type ID to duplicate / 要复制的图元类型ID"),
        newName: z.string().min(1).describe("The name for the new duplicated type / 新类型的名称"),
    }, async (args, extra) => {
        const params = {
            typeId: args.typeId,
            newName: args.newName,
        };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("duplicate_type", params);
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
                        text: `Duplicate type failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
