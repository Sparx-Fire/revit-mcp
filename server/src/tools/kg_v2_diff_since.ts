import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { callV2, mcpResult, mcpError } from "../kg-v2/client.js";
import { kgV2ToolsEnabled, logKgV2ModeOnce } from "../kg-v2/mode.js";

export function registerKgV2DiffSinceTool(server: McpServer) {
  logKgV2ModeOnce();
  if (!kgV2ToolsEnabled()) return;
  server.tool(
    "kg_v2_diff_since",
    "Return KG v2 action log entries with turn > since_turn (create/modify/delete/etc.). Use to know what changed since a checkpoint without re-querying the whole model. Read-only.",
    {
      since_turn: z
        .number()
        .int()
        .optional()
        .describe("Filter to entries strictly after this turn (default 0 = all history)."),
    },
    async (args: any) => {
      try {
        const r = await callV2<unknown>("kg_diff_since", {
          since_turn: args.since_turn ?? 0,
        });
        return mcpResult(r);
      } catch (e) {
        return mcpError(e);
      }
    }
  );
}
