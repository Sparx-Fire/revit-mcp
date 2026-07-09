import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetDocumentStylesTool(server: McpServer) {
  server.tool(
    "get_document_styles",
    "Return project annotation styles for AI-assisted drafting: dimension types, grid types, text note types (text height in mm), line patterns, graphics line styles, and title block types. Each style includes id, name, and key parameters so styles can be matched by template name or element id.",
    {},
    async () => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_document_styles", {});
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
              text: `get document styles failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
