import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerSetParametersTool(server) {
    server.tool("set_parameters", "Batch set parameters on a Revit element. Provide element ID and a key-value object of parameter names and values. / 批量设置Revit图元的参数。提供图元ID和参数名值对对象。", {
        elementId: z.number().int().describe("The element ID to set parameters on / 要设置参数的图元ID"),
        parameters: z.record(z.union([z.string(), z.number(), z.boolean()])).describe("Key-value pairs of parameter names and values (e.g. { \"Height\": 3000, \"Comment\": \"new\" }) / 参数名值对"),
    }, async (args, extra) => {
        const params = {
            elementId: args.elementId,
            parameters: args.parameters,
        };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("set_parameters", params);
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
                        text: `Set parameters failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
