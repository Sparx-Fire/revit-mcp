import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerManageProjectParametersTool(server: McpServer) {
  server.tool(
    "manage_project_parameters",
    "List or add shared parameters to a Revit project. Supports listing existing project parameters and binding shared parameters. / 列出或向Revit项目添加共享参数。支持列出已有项目参数和绑定共享参数。",
    {
      action: z.enum(["list", "add"]).describe("Action: 'list' to show existing project parameters, 'add' to bind shared parameters / 操作类型"),
      sharedParamFile: z.string().optional().describe("Path to shared parameter file (required for add action) / 共享参数文件路径（add操作必需）"),
      paramGroup: z.string().default("General").describe("Shared parameter group name (default: 'General') / 共享参数组名称"),
      params: z.array(z.object({
        name: z.string().describe("Shared parameter name / 共享参数名称"),
        categories: z.array(z.string()).optional().describe("Categories to bind the parameter to / 绑定到的类别列表"),
      })).optional().describe("List of shared parameters to add (required for add action) / 要添加的共享参数列表"),
    },
    async (args, extra) => {
      const params: any = {
        action: args.action,
      };
      if (args.sharedParamFile !== undefined) params.sharedParamFile = args.sharedParamFile;
      if (args.paramGroup !== undefined) params.paramGroup = args.paramGroup;
      if (args.params !== undefined) params.params = args.params;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("manage_project_parameters", params);
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
              text: `Manage project parameters failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
