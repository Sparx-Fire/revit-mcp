import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSetViewRangeTool(server: McpServer) {
  server.tool(
    "set_view_range",
    "Set the view range of a plan view in Revit (top, cut plane, bottom, view depth). All units in mm. / 在Revit中设置平面视图的视图范围（顶部、剖切面、底部、视图深度）。所有单位为毫米。",
    {
      viewId: z.number().int().describe("Plan view ID / 平面视图ID"),
      topOffset: z.number().optional().default(0).describe("Top offset in mm / 顶部偏移(mm)"),
      cutOffset: z.number().optional().default(1200).describe("Cut plane offset in mm / 剖切面偏移(mm)"),
      bottomOffset: z.number().optional().default(0).describe("Bottom offset in mm / 底部偏移(mm)"),
      viewDepthOffset: z.number().optional().default(0).describe("View depth offset in mm / 视图深度偏移(mm)"),
      topLevelId: z.number().int().optional().describe("Top level ID (optional) / 顶部标高ID（可选）"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("set_view_range", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Set view range failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
