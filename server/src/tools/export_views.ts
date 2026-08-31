import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerExportViewsTool(server: McpServer) {
  server.tool(
    "export_views",
    "Export Revit views to various formats (PNG, JPG, DWG, DXF, IFC, DGN).\n将 Revit 视图导出为多种格式（PNG、JPG、DWG、DXF、IFC、DGN）。",
    {
      data: z.array(z.object({
        viewIds: z.array(z.number()).describe("View IDs to export / 要导出的视图 ID 列表"),
        format: z.enum(["PNG", "JPG", "DWG", "DXF", "IFC", "DGN"]).describe("Export format / 导出格式"),
        folderPath: z.string().describe("Output folder path / 输出文件夹路径"),
        fileName: z.string().describe("Base file name / 基础文件名"),
      })).describe("Array of export tasks / 导出任务数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("export_views", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Export views failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
