import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerManageFamilyParametersTool(server: McpServer) {
  server.tool(
    "manage_family_parameters",
    "Add, rename, remove, or set formulas for family parameters in a Revit family document. / 在Revit族文档中添加、重命名、删除或设置族参数公式。",
    {
      action: z.enum(["add", "rename", "remove", "set_formula"]).describe("Action to perform: add, rename, remove, set_formula / 要执行的操作"),
      familyId: z.number().int().describe("The family element ID / 族图元ID"),
      name: z.string().optional().describe("Parameter name (required for all actions except list) / 参数名称"),
      newName: z.string().optional().describe("New name for rename action / 新名称（重命名操作）"),
      formula: z.string().optional().describe("Formula expression for set_formula action / 公式表达式（set_formula操作）"),
      type: z.string().optional().describe("Parameter type for add action (e.g. 'IFC_TYPE', 'IFC_LENGTH', 'IFC_TEXT') / 参数类型（添加操作）"),
    },
    async (args, extra) => {
      const params: any = {
        action: args.action,
        familyId: args.familyId,
      };
      if (args.name !== undefined) params.name = args.name;
      if (args.newName !== undefined) params.newName = args.newName;
      if (args.formula !== undefined) params.formula = args.formula;
      if (args.type !== undefined) params.type = args.type;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("manage_family_parameters", params);
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
              text: `Manage family parameters failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
