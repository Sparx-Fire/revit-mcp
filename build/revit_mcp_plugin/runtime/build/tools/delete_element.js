import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerDeleteElementTool(server) {
    server.tool("delete_element", "Delete one or more elements from the Revit model by their element IDs.", {
        elementIds: z
            .array(z.string())
            .describe("The IDs of the elements to delete"),
    }, async (args, extra) => {
        const params = {
            elementIds: args.elementIds,
        };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("delete_element", params);
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
                        text: `delete element failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
