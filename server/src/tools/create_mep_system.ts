import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateMepSystemTool(server: McpServer) {
  server.tool(
    "create_mep_system",
    "Create MEP systems (mechanical or piping) and assign elements to them. Supports supply air, return air, exhaust air, sanitary, hydronic supply, and hydronic return.\n创建 MEP 系统（机械或管道）并将图元分配给系统。支持送风、回风、排风、排污、供热供水、供热回水。",
    {
      data: z.array(z.object({
        systemType: z.enum(["SupplyAir", "ReturnAir", "ExhaustAir", "Sanitary", "HydronicSupply", "HydronicReturn"]).describe("System type / 系统类型"),
        name: z.string().describe("System name / 系统名称"),
        elementIds: z.array(z.number()).describe("Element IDs to include / 要包含的图元 ID 列表"),
      })).describe("Array of MEP systems to create / 要创建的 MEP 系统数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_mep_system", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Create MEP system failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
