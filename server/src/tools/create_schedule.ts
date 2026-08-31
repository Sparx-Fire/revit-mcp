import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateScheduleTool(server: McpServer) {
  server.tool(
    "create_schedule",
    "Create one or more schedules in Revit. Supports regular schedules, material takeoffs, key schedules, view lists, sheet lists, and revision schedules. Use category ID or name to specify the element category. / 在Revit中创建一个或多个明细表。支持常规明细表、材料统计、关键字明细表、视图列表、图纸列表和修订明细表。使用类别ID或名称指定图元类别。",
    {
      data: z
        .array(
          z.object({
            name: z
              .string()
              .optional()
              .describe("Schedule name / 明细表名称"),
            type: z
              .string()
              .optional()
              .describe(
                "Schedule type: regular, material, keynote, viewList, sheetList, revision / 明细表类型"
              ),
            categoryId: z
              .number()
              .optional()
              .describe("Category ID for the schedule / 明细表类别ID"),
            categoryName: z
              .string()
              .optional()
              .describe("Category name for the schedule / 明细表类别名称"),
            templateId: z
              .string()
              .optional()
              .describe("Template view ID to apply / 要应用的视图样板ID"),
            showTitle: z
              .boolean()
              .optional()
              .describe("Show schedule title / 显示明细表标题"),
            showHeaders: z
              .boolean()
              .optional()
              .describe("Show column headers / 显示列标题"),
            showGridLines: z
              .boolean()
              .optional()
              .describe("Show grid lines / 显示网格线"),
            showOutlines: z
              .boolean()
              .optional()
              .describe("Show outlines / 显示轮廓线"),
            fields: z
              .array(
                z.object({
                  parameterId: z.number().optional().describe("Parameter ID / 参数ID"),
                  parameterName: z.string().optional().describe("Parameter name / 参数名称"),
                  fieldType: z
                    .string()
                    .optional()
                    .describe("Field type: Instance, Type, Count, Formula / 字段类型"),
                  heading: z.string().optional().describe("Column heading / 列标题"),
                  isCalculatedField: z
                    .boolean()
                    .optional()
                    .describe("Whether it's a calculated field / 是否为计算字段"),
                  formula: z.string().optional().describe("Formula for calculated fields / 计算字段公式"),
                  width: z.number().optional().describe("Column width in pixels / 列宽(像素)"),
                  isHidden: z.boolean().optional().describe("Whether field is hidden / 是否隐藏字段"),
                  horizontalAlignment: z
                    .string()
                    .optional()
                    .describe("Alignment: Left, Center, Right / 对齐方式"),
                  formatOption: z.string().optional().describe("Format option / 格式选项"),
                  accuracy: z.number().optional().describe("Decimal accuracy / 小数精度"),
                  useThousandSeparator: z
                    .boolean()
                    .optional()
                    .describe("Use thousand separator / 使用千位分隔符"),
                })
              )
              .optional()
              .describe("Fields to include / 要包含的字段"),
            filters: z
              .array(
                z.object({
                  fieldName: z.string().optional().describe("Field name to filter by / 筛选字段名称"),
                  fieldIndex: z.number().optional().describe("Field index / 字段索引"),
                  filterType: z
                    .string()
                    .optional()
                    .describe("Filter type: Equal, NotEqual, GreaterThan, etc. / 筛选类型"),
                  filterValue: z.string().optional().describe("Filter value / 筛选值"),
                })
              )
              .optional()
              .describe("Filters to apply / 要应用的筛选条件"),
            clearExistingFilters: z
              .boolean()
              .optional()
              .describe("Clear existing filters before applying / 应用前清除现有筛选"),
            sortFields: z
              .array(
                z.object({
                  fieldName: z.string().optional().describe("Field name to sort by / 排序字段名称"),
                  fieldIndex: z.number().optional().describe("Field index / 字段索引"),
                  sortOrder: z
                    .string()
                    .optional()
                    .describe("Sort order: Ascending, Descending / 排序方式"),
                })
              )
              .optional()
              .describe("Sort fields / 排序字段"),
            clearExistingSorts: z
              .boolean()
              .optional()
              .describe("Clear existing sorts before applying / 应用前清除现有排序"),
            groupFields: z
              .array(
                z.object({
                  fieldName: z.string().optional().describe("Field name to group by / 分组字段名称"),
                  fieldIndex: z.number().optional().describe("Field index / 字段索引"),
                  sortOrder: z
                    .string()
                    .optional()
                    .describe("Sort order: Ascending, Descending / 排序方式"),
                  showHeader: z.boolean().optional().describe("Show group header / 显示组标题"),
                  showFooter: z.boolean().optional().describe("Show group footer / 显示组页脚"),
                  showBlankLine: z.boolean().optional().describe("Show blank line after group / 组后显示空行"),
                })
              )
              .optional()
              .describe("Group fields / 分组字段"),
            clearExistingGroups: z
              .boolean()
              .optional()
              .describe("Clear existing groups before applying / 应用前清除现有分组"),
            parameters: z
              .record(z.any())
              .optional()
              .describe("Additional schedule parameters / 附加明细表参数"),
          })
        )
        .describe("Array of schedules to create / 要创建的明细表数组"),
    },
    async (args, extra) => {
      const params = args;

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
