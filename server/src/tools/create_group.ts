import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateGroupTool(server: McpServer) {
  server.tool(
    "create_group",
    "Create element groups in the Revit model. Groups selected elements by their IDs with a specified group name.\n在 Revit 中创建图元组。通过指定的图元 ID 列表和组名称创建图元组。",
    {
      data: z.array(z.object({
        elementIds: z.array(z.number()).describe("Element IDs to include in the group / 要包含在图元组中的图元 ID 列表"),
        groupName: z.string().describe("Name for the new group / 新图元组的名称"),
      })).describe("Array of groups to create / 要创建的图元组数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_group", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create group failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
