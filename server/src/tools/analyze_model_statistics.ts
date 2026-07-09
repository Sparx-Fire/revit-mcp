import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
import { modelStatisticsCache } from "../utils/ModelCache.js";

function asStatisticsResult(
  data: Record<string, unknown>,
  cached: boolean
): Record<string, unknown> {
  return { ...data, cached };
}

export function registerAnalyzeModelStatisticsTool(server: McpServer) {
  server.tool(
    "analyze_model_statistics",
    "Analyze model complexity with element counts. Returns detailed statistics about the Revit model including total element counts, total types, total families, views, sheets, counts by category (with type/family breakdown), and level-by-level element distribution. Useful for model auditing, performance analysis, and understanding model composition.",
    {
      includeDetailedTypes: z
        .boolean()
        .optional()
        .default(true)
        .describe("Whether to include detailed breakdown by family and type within each category. Defaults to true."),
    },
    async (args, extra) => {
      const includeDetailedTypes = args.includeDetailedTypes ?? true;
      const params = { includeDetailedTypes };

      try {
        const lastKnownProject = modelStatisticsCache.getLastKnownProjectName();
        if (lastKnownProject) {
          const cached = modelStatisticsCache.get(
            lastKnownProject,
            includeDetailedTypes
          );
          if (cached) {
            return {
              content: [
                {
                  type: "text",
                  text: JSON.stringify(asStatisticsResult(cached, true), null, 2),
                },
              ],
            };
          }
        }

        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("analyze_model_statistics", params);
        });

        const result: Record<string, unknown> =
          response && typeof response === "object"
            ? (response as Record<string, unknown>)
            : { data: response };

        const projectName =
          typeof result.projectName === "string" ? result.projectName : null;

        if (
          projectName &&
          lastKnownProject &&
          projectName !== lastKnownProject
        ) {
          modelStatisticsCache.invalidate(lastKnownProject);
        }

        if (projectName) {
          modelStatisticsCache.set(projectName, includeDetailedTypes, result);
        }

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(asStatisticsResult(result, false), null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Analyze model statistics failed: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}
