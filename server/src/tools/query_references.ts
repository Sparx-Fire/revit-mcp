import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerQueryReferencesTool(server: McpServer) {
  server.tool(
    "query_references",
    "Get stable geometric references of a Revit element for dimensioning and tagging. Returns face and edge references. / 获取Revit图元的稳定几何引用，用于标注和标记。返回面和边的引用。",
    {
      elementId: z.number().int().describe("The element ID to get references for / 要获取引用的图元ID"),
    },
    async (args, extra) => {
      const params = { elementId: args.elementId };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("query_references", params);
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(response, null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Query references failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
