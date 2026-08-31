import { z } from "zod";
import { withRevitConnection } from "../utils/ConnectionManager.js";
export function registerQueryParametersTool(server) {
    server.tool("query_parameters", "Query all parameters of a Revit element by element ID. Returns parameter name, value, and storage type for each parameter. / 查询Revit图元的所有参数，返回参数名称、值和存储类型。", {
        elementId: z.number().int().describe("The element ID to query parameters for / 要查询参数的图元ID"),
    }, async (args, extra) => {
        const params = { elementId: args.elementId };
        try {
            const response = await withRevitConnection(async (revitClient) => {
                return await revitClient.sendCommand("query_parameters", params);
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
                        text: `Query parameters failed: ${error instanceof Error ? error.message : String(error)}`,
                    },
                ],
            };
        }
    });
}
