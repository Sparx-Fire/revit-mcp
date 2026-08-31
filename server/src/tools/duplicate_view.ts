import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerDuplicateViewTool(server: McpServer) {
  server.tool(
    "duplicate_view",
    "Duplicate a view in Revit with specified duplication mode. / 在Revit中以指定的复制方式复制视图。",
    {
      viewId: z.number().int().describe("Source view ID to duplicate / 要复制的源视图ID"),
      mode: z.enum(["duplicate", "with_detailing", "dependent"]).optional().default("duplicate").describe("Duplication mode / 复制方式"),
      newName: z.string().optional().describe("New view name (optional) / 新视图名称（可选）"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("duplicate_view", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Duplicate view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
