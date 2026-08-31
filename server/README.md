# mcp-server-for-revit

MCP server for interacting with Autodesk Revit through AI assistants like Claude.

This package is the MCP server component of [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit). It exposes Revit operations as MCP tools that AI clients can call. The server communicates with the [Revit plugin](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) over WebSocket to execute commands inside Revit.

> [!NOTE]
> This server requires the mcp-servers-for-revit Revit plugin to be installed and running inside Revit. See the [full project README](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) for setup instructions.

## Setup

**Claude Code**

```bash
claude mcp add mcp-server-for-revit -- npx -y mcp-server-for-revit
```

**Claude Desktop**

Claude Desktop → Settings → Developer → Edit Config → `claude_desktop_config.json`:

```json
{
    "mcpServers": {
        "mcp-server-for-revit": {
            "command": "npx",
            "args": ["-y", "mcp-server-for-revit"]
        }
    }
}
```

Restart Claude Desktop. When you see the hammer icon, the MCP server is connected.

## Supported Tools (90 total)

This server exposes **84 Revit commands** + **6 utility tools**. See the [main project README](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit) for the full categorized list.

### Revit Commands (84)

All Revit commands are defined in `Commands/RevitMCPCommandSet/command.json` and registered via `Commands/commandRegistry.json` at runtime. The plugin uses `{VERSION}` placeholder substitution to select the correct DLL for the current Revit version.

**Categories:** General (3) · Query & Selection (10) · Architecture (19) · MEP (10) · Annotation (8) · Views & Sheets (18) · Modify (10) · Family (2) · Analysis & Data (4)

### Database Utilities (3)

| Tool | Description |
| ---- | ----------- |
| `store_project_data` | Store project metadata in local database |
| `store_room_data` | Store room metadata in local database |
| `query_stored_data` | Query stored project and room data |

### Other Utilities (3)

| Tool | Description |
| ---- | ----------- |
| `search_modules` | Search for available modules |
| `use_module` | Use a specific module |
| `modify_element` | Generic element modification helper |

## Development

```bash
npm install
npm run build
```

## License

[MIT](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit/blob/main/LICENSE)
