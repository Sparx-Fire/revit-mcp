import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

export async function registerTools(server: McpServer) {
  // Get this file's directory
  const __filename = fileURLToPath(import.meta.url);
  const __dirname = path.dirname(__filename);

  // Read all files in the tools directory
  const files = fs.readdirSync(__dirname);

  // Keep .ts/.js files, excluding index and register
  const toolFiles = files.filter(
    (file) =>
      (file.endsWith(".ts") || file.endsWith(".js")) &&
      file !== "index.ts" &&
      file !== "index.js" &&
      file !== "register.ts" &&
      file !== "register.js"
  );

  // Dynamically import and register each tool
  for (const file of toolFiles) {
    try {
      // Build the import path
      const importPath = `./${file.replace(/\.(ts|js)$/, ".js")}`;

      // Import the module dynamically
      const module = await import(importPath);

      // Find and invoke the register function
      const registerFunctionName = Object.keys(module).find(
        (key) => key.startsWith("register") && typeof module[key] === "function"
      );

      if (registerFunctionName) {
        module[registerFunctionName](server);
        console.error(`Registered tool: ${file}`);
      } else {
        console.warn(`Warning: no register function found in ${file}`);
      }
    } catch (error) {
      console.error(`Error registering tool ${file}:`, error);
    }
  }
}
