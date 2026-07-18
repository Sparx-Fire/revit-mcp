#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerTools } from "./tools/register.js";

// Create the server instance
const server = new McpServer({
  name: "mcp-server-for-revit",
  version: "1.0.0",
});

// Start the server
async function main() {
  // Register tools
  await registerTools(server);

  // Connect to the transport
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Revit MCP Server start success");
}

main().catch((error) => {
  console.error("Error starting Revit MCP Server:", error);
  process.exit(1);
});
