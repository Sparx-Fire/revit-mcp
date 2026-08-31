import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSaveDocumentTool(server: McpServer) {
  server.tool(
    "save_document",
    "Save the current Revit document. / 保存当前Revit文档。",
    {},
    async (args, extra) => {
      const params = {};
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("save_document", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Save document failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
