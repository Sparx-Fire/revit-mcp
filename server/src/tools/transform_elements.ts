import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerTransformElementsTool(server: McpServer) {
  server.tool(
    "transform_elements",
    "Move, copy, rotate, or mirror Revit elements. Returns new element IDs for copy operations. / 移动、复制、旋转或镜像Revit图元。复制操作返回新图元ID。",
    {
      elementIds: z.array(z.number().int().positive()).min(1).describe("Array of element IDs to transform / 要变换的图元ID数组"),
      transformType: z.enum(["move", "copy", "rotate", "mirror"]).describe("Type of transform: move, copy, rotate, or mirror / 变换类型"),
      params: z.object({
        dx: z.number().optional().describe("Translation X in feet (for move/copy) / X方向平移（英尺）"),
        dy: z.number().optional().describe("Translation Y in feet (for move/copy) / Y方向平移（英尺）"),
        dz: z.number().optional().describe("Translation Z in feet (for move/copy) / Z方向平移（英尺）"),
        angle: z.number().optional().describe("Rotation angle in radians (for rotate) / 旋转角度（弧度）"),
        axis: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe("Rotation axis vector (for rotate) / 旋转轴向量"),
        origin: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe("Origin point for rotate/mirror / 旋转或镜像原点"),
        normal: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe("Mirror plane normal (for mirror) / 镜像平面法线"),
      }).describe("Transform parameters / 变换参数"),
    },
    async (args, extra) => {
      const params = {
        elementIds: args.elementIds,
        transformType: args.transformType,
        params: args.params,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("transform_elements", params);
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
              text: `Transform elements failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
