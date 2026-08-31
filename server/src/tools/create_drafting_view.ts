import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateDraftingViewTool(server: McpServer) {
  server.tool(
    "create_drafting_view",
    "Create a drafting view in Revit with custom name, scale, and detail level. / 在Revit中创建带有自定义名称、比例和详细程度的绘图视图。",
    {
      name: z.string().optional().describe("View name / 视图名称"),
      scale: z.number().int().optional().default(100).describe("View scale (e.g. 100 for 1:100) / 视图比例"),
      detailLevel: z.enum(["Coarse", "Medium", "Fine"]).optional().default("Coarse").describe("Detail level / 详细程度"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_drafting_view", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create drafting view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
