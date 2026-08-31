import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateViewTool(server: McpServer) {
  server.tool(
    "create_view",
    "Create one or more views in Revit such as floor plans, ceiling plans, sections, elevations, or 3D views. Supports view type selection, level assignment, scale, detail level, and view template application. All units in millimeters (mm). / 在Revit中创建一个或多个视图，如楼层平面、天花平面、剖面、立面或三维视图。支持视图类型选择、标高指定、比例、详细程度和视图模板应用。所有单位为毫米(mm)。",
    {
      data: z
        .array(
          z.object({
            name: z
              .string()
              .optional()
              .describe("View name / 视图名称"),
            viewType: z
              .string()
              .optional()
              .describe(
                "View type: FloorPlan, CeilingPlan, Elevation, Section, 3D / 视图类型"
              ),
            levelElevation: z
              .number()
              .optional()
              .describe(
                "Level elevation in mm (for plan/section/elevation views) / 标高高度(mm)"
              ),
            detailLevel: z
              .string()
              .optional()
              .describe("Detail level: Coarse, Medium, Fine / 详细程度"),
            scale: z
              .number()
              .optional()
              .describe("View scale (e.g., 100 for 1:100) / 视图比例"),
            viewFamilyTypeName: z
              .string()
              .optional()
              .describe("View family type name / 视图族类型名称"),
            templateId: z
              .string()
              .optional()
              .describe("Template view ID to apply / 要应用的视图样板ID"),
            direction: z
              .object({
                x: z.number().optional().describe("X direction component / X方向分量"),
                y: z.number().optional().describe("Y direction component / Y方向分量"),
                z: z.number().optional().describe("Z direction component / Z方向分量"),
              })
              .optional()
              .describe(
                "View direction for elevation/section views / 立面/剖面的视图方向"
              ),
            parameters: z
              .record(z.any())
              .optional()
              .describe(
                "Additional view parameters / 附加视图参数"
              ),
          })
        )
        .describe("Array of views to create / 要创建的视图数组"),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_view", params);
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
              text: `Create view failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
