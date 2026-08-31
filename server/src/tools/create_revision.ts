import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateRevisionTool(server: McpServer) {
  server.tool(
    "create_revision",
    "Create a revision in Revit with name, date, number, and description. / 在Revit中创建带有名称、日期、编号和描述的修订。",
    {
      name: z.string().describe("Revision name/description / 修订名称/描述"),
      date: z.string().optional().describe("Revision date / 修订日期"),
      number: z.string().optional().describe("Revision number / 修订编号"),
      description: z.string().optional().describe("Additional description / 附加描述"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_revision", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create revision failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
