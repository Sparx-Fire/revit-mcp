import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateSpaceTool(server: McpServer) {
  server.tool(
    "create_space",
    "Create spaces in the Revit model for MEP analysis. All units in mm.\n在 Revit 模型中创建空间用于 MEP 分析。所有单位为毫米。",
    {
      data: z.array(z.object({
        name: z.string().describe("Space name / 空间名称"),
        number: z.string().describe("Space number / 空间编号"),
        location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Insertion point in mm / 插入点坐标（毫米）"),
        baseLevel: z.number().describe("Base level elevation in mm / 基准标高（毫米）"),
        spaceType: z.string().optional().describe("Space type / 空间类型"),
      })).describe("Array of spaces to create / 要创建的空间数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_space", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create space failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
