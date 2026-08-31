import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerManageViewFiltersTool(server: McpServer) {
  server.tool(
    "manage_view_filters",
    "Add or remove view filters in Revit and optionally set graphic overrides. / 在Revit中添加或移除视图过滤器，并可选择设置图形覆盖。",
    {
      viewId: z.number().int().describe("View ID to manage filters on / 要管理过滤器的视图ID"),
      action: z.enum(["add", "remove"]).describe("Action: add or remove / 操作：添加或移除"),
      filterName: z.string().describe("Filter name / 过滤器名称"),
      overrides: z.object({
        visible: z.boolean().optional().describe("Filter visibility / 过滤器可见性"),
        color: z.object({
          r: z.number().int().min(0).max(255),
          g: z.number().int().min(0).max(255),
          b: z.number().int().min(0).max(255),
        }).optional().describe("Override color / 覆盖颜色"),
        lineWeight: z.number().int().optional().describe("Override line weight / 覆盖线宽"),
        fillPattern: z.string().optional().describe("Override fill pattern name / 覆盖填充图案名称"),
        halftone: z.boolean().optional().describe("Halftone / 半色调"),
      }).optional().describe("Filter graphic overrides (for add action) / 过滤器图形覆盖（添加操作）"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("manage_view_filters", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Manage view filters failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
