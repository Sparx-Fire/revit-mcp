import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerRenameElementTool(server: McpServer) {
  server.tool(
    "rename_element",
    "Rename a Revit element. Works on elements with editable Name parameters, levels, grids, and element types. / 重命名Revit图元。适用于具有可编辑Name参数的图元、标高、轴网和族类型。",
    {
      elementId: z.number().int().describe("The element ID to rename / 要重命名的图元ID"),
      newName: z.string().min(1).describe("The new name for the element / 图元的新名称"),
    },
    async (args, extra) => {
      const params = {
        elementId: args.elementId,
        newName: args.newName,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("rename_element", params);
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
              text: `Rename element failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
