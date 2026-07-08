import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const categorySchema = z.enum(["Doors", "Windows", "Floors"]);

export function registerValidateScheduleTool(server: McpServer) {
  server.tool(
    "validate_schedule",
    "Compare a Revit schedule against model elements for Doors, Windows, or Floors. Returns modelCount, scheduleCount, diff, and missingIds for elements present in the model but absent from the schedule. Optionally filter by schedule name and/or level.",
    {
      category: categorySchema.describe(
        "Element category to validate: Doors, Windows, or Floors."
      ),
      scheduleName: z
        .string()
        .optional()
        .describe(
          "Optional schedule view name. If omitted, the first schedule for the category is used."
        ),
      levelName: z
        .string()
        .optional()
        .describe("Optional level name to limit comparison to elements on that level."),
      levelId: z
        .number()
        .int()
        .optional()
        .describe("Optional level element id. Alternative to levelName."),
    },
    async (args) => {
      const params = {
        category: args.category,
        scheduleName: args.scheduleName ?? null,
        levelName: args.levelName ?? null,
        levelId: args.levelId ?? null,
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("validate_schedule", params);
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
              text: `Validate schedule failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
