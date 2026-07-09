import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetCurrentViewElementsTool(server: McpServer) {
  server.tool(
    "get_current_view_elements",
    "Get elements from the current active view in Revit. You can filter by model categories (like Walls, Floors) or annotation categories (like Dimensions, Text). Use includeHidden to show/hide invisible elements. Results are paginated: by default limit=500 elements per page; use offset for subsequent pages. Response includes totalCount and hasMore for large models.",
    {
      modelCategoryList: z
        .array(z.string())
        .optional()
        .describe(
          "List of Revit model category names (e.g., 'OST_Walls', 'OST_Doors', 'OST_Floors')"
        ),
      annotationCategoryList: z
        .array(z.string())
        .optional()
        .describe(
          "List of Revit annotation category names (e.g., 'OST_Dimensions', 'OST_WallTags', 'OST_TextNotes')"
        ),
      includeHidden: z
        .boolean()
        .optional()
        .describe("Whether to include hidden elements in the results"),
      limit: z
        .number()
        .int()
        .positive()
        .optional()
        .describe("Maximum number of elements to return per page. Default is 500."),
      offset: z
        .number()
        .int()
        .min(0)
        .optional()
        .describe("Number of matching elements to skip before returning results. Default is 0. Use with limit for paginated loading."),
    },
    async (args, extra) => {
      const params: Record<string, unknown> = {
        includeHidden: args.includeHidden ?? false,
      };
      if (args.modelCategoryList?.length) {
        params.modelCategoryList = args.modelCategoryList;
      }
      if (args.annotationCategoryList?.length) {
        params.annotationCategoryList = args.annotationCategoryList;
      }
      if (args.limit !== undefined) {
        params.limit = args.limit;
      }
      if (args.offset !== undefined) {
        params.offset = args.offset;
      }

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "get_current_view_elements",
            params
          );
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
              text: `get current view elements failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
