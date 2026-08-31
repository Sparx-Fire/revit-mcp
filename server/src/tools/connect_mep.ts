import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerConnectMepTool(server: McpServer) {
  server.tool(
    "connect_mep",
    "Connect two MEP elements via their connectors. Supports direct, elbow, tee, reducer, and cross connections.\n通过连接器连接两个 MEP 图元。支持直接连接、弯头、三通、变径和十字连接。",
    {
      data: z.array(z.object({
        elementId1: z.number().describe("First element ID / 第一个图元 ID"),
        elementId2: z.number().describe("Second element ID / 第二个图元 ID"),
        connectorIndex1: z.number().optional().describe("Connector index on first element / 第一个图元上的连接器索引"),
        connectorIndex2: z.number().optional().describe("Connector index on second element / 第二个图元上的连接器索引"),
        connectType: z.enum(["Direct", "Elbow", "Tee", "Reducer", "Cross"]).optional().describe("Connection type / 连接类型"),
      })).describe("Array of connections to make / 要创建的连接数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("connect_mep", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Connect MEP failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
