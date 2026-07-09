import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export const MAX_BATCH_SIZE = 20;

const batchCommandSchema = z.object({
  command: z.string().min(1).describe("Revit command name (e.g. get_current_view_info)"),
  params: z
    .record(z.unknown())
    .optional()
    .default({})
    .describe("Parameters object for the command"),
});

export function registerBatchExecuteTool(server: McpServer) {
  server.tool(
    "batch_execute",
    `Execute up to ${MAX_BATCH_SIZE} Revit commands in a single round-trip. Commands run sequentially in Revit. If one command fails, the rest still execute (partial success).`,
    {
      commands: z
        .array(batchCommandSchema)
        .min(1)
        .max(MAX_BATCH_SIZE)
        .describe(
          `Array of commands to run in order. Maximum ${MAX_BATCH_SIZE} commands per batch.`
        ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("batch_execute", {
            commands: args.commands,
          });
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
              text: `batch_execute failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
