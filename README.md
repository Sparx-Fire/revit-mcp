[![Cover Image](./assets/cover.png)](https://github.com/sparx-fire/revit-mcp)

# revit-mcp

English | [简体中文](README_zh.md)

## Description

> [!NOTE]
> This is a fork of the original [revit-mcp](https://github.com/mcp-servers-for-revit/revit-mcp) project with additional tools and functionality improvements. These changes have not been through the mseep.ai auditing process.

revit-mcp allows you to interact with Revit using the MCP protocol through MCP-supported clients (such as Claude, Cline, etc.).

This project is the server side (providing Tools to AI), and you need to use [revit-mcp-plugin](https://github.com/sparx-fire/revit-mcp-plugin) (driving Revit) in conjunction.

## Features

- Allow AI to get data from the Revit project
- Allow AI to drive Revit to create, modify, and delete elements
- Send AI-generated code to Revit to execute (may not be successful, successful rate is higher in some simple scenarios with clear requirements)

## Requirements

- nodejs 18+

> Complete installation environment still needs to consider the needs of revit-mcp-plugin, please refer to [revit-mcp-plugin](https://github.com/sparx-fire/revit-mcp-plugin)

## Installation

**Claude Code**

```bash
claude mcp add revit-mcp -- npx -y @sparx-fire/revit-mcp
```

**Claude Desktop**

Claude Desktop -> Settings -> Developer -> Edit Config -> claude_desktop_config.json

```json
{
    "mcpServers": {
        "revit-mcp": {
            "command": "npx",
            "args": ["-y", "@sparx-fire/revit-mcp"]
        }
    }
}
```

Restart Claude Desktop. When you see the hammer icon, it means the connection to the MCP service is normal.

![claude](./assets/claude.png)

## Framework

```mermaid
flowchart LR
	ClaudeDesktop --> revit-mcp --> SocketService--commandName-->CommandSet--command-->CommandExecute
	CommandManager --> CommandSet
	CommandExecute --executeResult--> SocketService
	CommandProject1 --> CommandManager
	CommandProject2 --> CommandManager
	CommandProject... --> CommandManager
	subgraph ide1 [MCPClient]
	ClaudeDesktop
	end
	subgraph ide2 [MCPServer]
	revit-mcp
	end
	subgraph ide3 [Revit]
			subgraph ide3.1 [revit-mcp-plugin]
				SocketService
				CommandSet
				CommandManager
				CommandExecute
			end
	end
```

## Supported Tools

| Name | Description |
| ---- | ----------- |
| get_current_view_info | Get current active view info |
| get_current_view_elements | Get elements from the current active view |
| get_available_family_types | Get available family types in current project |
| get_selected_elements | Get currently selected elements |
| get_material_quantities | Calculate material quantities and takeoffs |
| ai_element_filter | Intelligent element querying tool for AI assistants |
| analyze_model_statistics | Analyze model complexity with element counts |
| create_point_based_element | Create point-based elements (door, window, furniture) |
| create_line_based_element | Create line-based elements (wall, beam, pipe) |
| create_surface_based_element | Create surface-based elements (floor, ceiling, roof) |
| create_grid | Create a grid system with smart spacing generation |
| create_level | Create levels at specified elevations |
| create_room | Create and place rooms at specified locations |
| create_dimensions | Create dimension annotations in the current view |
| create_structural_framing_system | Create a structural beam framing system |
| delete_element | Delete elements by ID |
| operate_element | Operate on elements (select, setColor, hide, etc.) |
| color_elements | Color elements based on a parameter value |
| tag_all_walls | Tag all walls in the current view |
| tag_all_rooms | Tag all rooms in the current view |
| export_room_data | Export all room data from the project |
| store_project_data | Store project metadata in local database |
| store_room_data | Store room metadata in local database |
| query_stored_data | Query stored project and room data |
| send_code_to_revit | Send C# code to Revit to execute |
| say_hello | Display a greeting dialog in Revit (connection test) |
