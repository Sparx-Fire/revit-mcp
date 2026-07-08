import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const scheduleFieldSchema = z.object({
  parameterId: z.number().optional().default(0),
  parameterName: z.string().optional().default(""),
  fieldType: z
    .enum(["Instance", "Type", "ElementType", "Count", "Formula", "Phasing"])
    .optional()
    .default("Instance"),
  heading: z.string().optional().default(""),
  isCalculatedField: z.boolean().optional().default(false),
  formula: z.string().optional().default(""),
  width: z.number().optional().default(0),
  isHidden: z.boolean().optional().default(false),
  horizontalAlignment: z
    .enum(["Left", "Center", "Right"])
    .optional()
    .default("Left"),
  formatOption: z.string().optional().default(""),
  accuracy: z.number().optional(),
  useThousandSeparator: z.boolean().optional(),
});

const scheduleFilterSchema = z.object({
  fieldName: z.string(),
  fieldIndex: z.number().optional().default(-1),
  filterType: z
    .enum([
      "Equal",
      "NotEqual",
      "GreaterThan",
      "GreaterThanOrEqual",
      "LessThan",
      "LessThanOrEqual",
      "Contains",
      "NotContains",
      "BeginsWith",
      "EndsWith",
    ])
    .optional()
    .default("Equal"),
  filterValue: z.string().optional().default(""),
});

const scheduleSortSchema = z.object({
  fieldName: z.string(),
  fieldIndex: z.number().optional().default(-1),
  sortOrder: z.enum(["Ascending", "Descending"]).optional().default("Ascending"),
});

const scheduleGroupSchema = z.object({
  fieldName: z.string(),
  fieldIndex: z.number().optional().default(-1),
  sortOrder: z.enum(["Ascending", "Descending"]).optional().default("Ascending"),
  showHeader: z.boolean().optional().default(true),
  showFooter: z.boolean().optional().default(false),
  showBlankLine: z.boolean().optional().default(false),
  formatData: z.string().optional().default(""),
});

const scheduleSchema = z.object({
  name: z.string().describe("Schedule view name"),
  type: z
    .enum(["Regular", "KeySchedule", "MaterialTakeoff"])
    .optional()
    .default("Regular"),
  categoryId: z.number().optional().default(0),
  categoryName: z
    .string()
    .optional()
    .default("")
    .describe("Category name such as Doors, Windows, Rooms, or OST_Doors"),
  templateId: z
    .string()
    .optional()
    .default("")
    .describe("UniqueId or ElementId of an existing schedule template in the project"),
  showTitle: z.boolean().optional(),
  showHeaders: z.boolean().optional(),
  showGridLines: z.boolean().optional(),
  showOutlines: z.boolean().optional(),
  fields: z.array(scheduleFieldSchema).optional().default([]),
  filters: z.array(scheduleFilterSchema).optional().default([]),
  clearExistingFilters: z.boolean().optional().default(true),
  sortFields: z.array(scheduleSortSchema).optional().default([]),
  clearExistingSorts: z.boolean().optional().default(true),
  groupFields: z.array(scheduleGroupSchema).optional().default([]),
  clearExistingGroups: z.boolean().optional().default(true),
  parameters: z.record(z.unknown()).optional(),
});

export function registerCreateScheduleTool(server: McpServer) {
  server.tool(
    "create_schedule",
    "Create a Revit ViewSchedule from project template and ScheduleCreationInfo. Supports Doors/Windows/Rooms categories, optional templateId duplication, fields, filters, sorting, and grouping. Replaces send_code_to_revit for schedule creation.",
    {
      schedule: scheduleSchema.describe(
        "Schedule creation settings. Use categoryName Doors, Windows, or Rooms and templateId from an existing project schedule template."
      ),
    },
    async (args) => {
      const params = { schedule: args.schedule };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_schedule", params);
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
              text: `Create schedule failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
