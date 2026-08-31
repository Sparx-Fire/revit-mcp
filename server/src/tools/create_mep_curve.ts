import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateMEPCurveTool(server: McpServer) {
  server.tool(
    "create_mep_curve",
    "Create MEP curves in Revit (duct/pipe/conduit). All units in mm. / 在Revit中创建MEP曲线（风管/管道/线管）。所有单位为毫米。",
    {
      mepType: z.enum(["duct", "pipe", "conduit"]).describe("MEP element type / MEP图元类型"),
      start: z.object({
        x: z.number().describe("Start X in mm / 起点X(mm)"),
        y: z.number().describe("Start Y in mm / 起点Y(mm)"),
        z: z.number().describe("Start Z in mm / 起点Z(mm)"),
      }).describe("Start point in mm / 起点(mm)"),
      end: z.object({
        x: z.number().describe("End X in mm / 终点X(mm)"),
        y: z.number().describe("End Y in mm / 终点Y(mm)"),
        z: z.number().describe("End Z in mm / 终点Z(mm)"),
      }).describe("End point in mm / 终点(mm)"),
      level: z.number().describe("Level elevation in mm / 标高(mm)"),
      diameter: z.number().optional().default(200).describe("Diameter in mm / 直径(mm)"),
      systemType: z.string().optional().describe("System type name / 系统类型名称"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_mep_curve", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create MEP curve failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
