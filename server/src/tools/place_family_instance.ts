import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerPlaceFamilyInstanceTool(server: McpServer) {
  server.tool(
    "place_family_instance",
    "Place a family instance by FamilySymbol ID with support for unhosted, hosted, face-based, and workplane placement. Optional rotation. All units in mm.\n通过 FamilySymbol ID 放置族实例，支持无宿主、有宿主、基于面和基于工作平面的放置。可选旋转。所有单位为毫米。",
    {
      data: z.array(z.object({
        symbolId: z.number().describe("FamilySymbol ID / 族符号 ID"),
        placementType: z.enum(["Unhosted", "Hosted", "FaceBased", "Workplane"]).describe("Placement type / 放置类型"),
        location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Insertion point in mm / 插入点坐标（毫米）"),
        hostId: z.number().optional().describe("Host element ID (for Hosted type) / 宿主图元 ID（用于有宿主类型）"),
        level: z.number().optional().describe("Level elevation in mm / 标高（毫米）"),
        rotation: z.number().optional().describe("Rotation in degrees around Z-axis / 绕Z轴旋转角度（度）"),
      })).describe("Array of family instances to place / 要放置的族实例数组"),
    },
    async (args, extra) => {
      const params = args;
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("place_family_instance", params);
        });
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Place family instance failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
