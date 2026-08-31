[![Cover Image](./assets/cover.png?v=2)](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit)

# mcp-servers-for-revit

**Connect AI assistants to Autodesk Revit via the Model Context Protocol.**

mcp-servers-for-revit enables AI clients like Claude, Cline, and other MCP-compatible tools to read, create, modify, and delete elements in Revit projects. It consists of three components: a TypeScript MCP server that exposes tools to AI, a C# Revit add-in that bridges commands into Revit, and a command set that implements the actual Revit API operations.

> [!NOTE]
> This is a fork of the original [revit-mcp](https://github.com/mcp-servers-for-revit/revit-mcp) project with additional tools and functionality improvements.

## Architecture

```mermaid
flowchart LR
    Client["MCP Client<br/>(Claude, Cline, etc.)"]
    Server["MCP Server<br/><code>server/</code>"]
    Plugin["Revit Plugin<br/><code>plugin/</code>"]
    CommandSet["Command Set<br/><code>commandset/</code>"]
    Revit["Revit API"]

    Client <-->|stdio| Server
    Server <-->|TCP/JSON-RPC<br/>port 8080| Plugin
    Plugin -->|loads| CommandSet
    CommandSet -->|executes| Revit
```

The **MCP Server** (TypeScript) translates tool calls from AI clients into TCP/JSON-RPC messages on port 8080. The **Revit Plugin** (C#) runs inside Revit, listens for those messages on a `TcpListener`, and dispatches them to the **Command Set** (C#), which executes the actual Revit API operations and returns results back up the chain.

## Requirements

- **Autodesk Revit 2020 - 2026** (any supported version)
- **Node.js 18+** (only needed when building from source)

## Installation

### Option 1: ZIP Package (Recommended)

Download the ZIP for your Revit version from the [Releases](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit/releases) page, then:

```
Extract ZIP → right-click install.ps1 → Run with PowerShell
```

The package includes a bundled Node.js runtime, so **no separate Node.js installation is required**.

### Option 2: Manual Install (From Release ZIP)

Extract the ZIP and copy to your Revit addins folder:
```
%AppData%\Autodesk\Revit\Addins\<your Revit version>\
```

Expected structure:
```
Addins/2026/
├── mcp-servers-for-revit.addin
└── revit_mcp_plugin/
    ├── RevitMCPPlugin.dll
    ├── runtime/           ← bundled Node.js + MCP server
    │   ├── node.exe
    │   └── build/index.js
    └── Commands/
        └── RevitMCPCommandSet/
            ├── command.json
            └── 2026/
                └── RevitMCPCommandSet.dll
```

### Option 3: Build from Source

```powershell
.\build.ps1 -RevitVersion R26     # Build command set + bundle runtime
.\install.ps1                      # Deploy to %AppData%
```

Use `-SkipServer` to skip rebuilding the MCP server if you only changed C# code:
```powershell
.\build.ps1 -RevitVersion R26 -SkipServer
```

### MCP Server Setup

After installation, configure your AI client to use the local runtime (no separate Node.js or npm needed):

**Claude Code**
```bash
claude mcp add mcp-server-for-revit -- "%APPDATA%/Autodesk/Revit/Addins/2026/revit_mcp_plugin/runtime/node.exe" "%APPDATA%/Autodesk/Revit/Addins/2026/revit_mcp_plugin/runtime/build/index.js"
```

**Claude Desktop**
```json
{
    "mcpServers": {
        "mcp-server-for-revit": {
            "command": "%APPDATA%/Autodesk/Revit/Addins/2026/revit_mcp_plugin/runtime/node.exe",
            "args": ["%APPDATA%/Autodesk/Revit/Addins/2026/revit_mcp_plugin/runtime/build/index.js"]
        }
    }
}
```

> Replace `2026` with your Revit version year.

Restart Claude Desktop. When you see the hammer icon, the MCP server is connected.

![Claude Desktop connection](./assets/claude.png)

### Starting the Service

1. Start Revit — if prompted about an unknown add-in, click **Always Load**
2. The plugin automatically starts the TCP service on port 8080. No manual activation is required.
3. Verify the connection with any MCP client tool (e.g. `say_hello`).

## Revit Plugin Setup

If using a release ZIP, the plugin is already included. For manual installation:

1. Build the plugin from `plugin/` (see [Development](#development))
2. Copy `mcp-servers-for-revit.addin` to `%AppData%\Autodesk\Revit\Addins\<version>\`
3. Copy the `revit_mcp_plugin/` folder to the same addins directory

## Command Set Setup

If using a release ZIP, the command set is pre-installed inside the plugin. For manual installation:

1. Build the command set from `commandset/` (see [Development](#development))
2. Inside the plugin's installation directory, create `Commands/RevitMCPCommandSet/<year>/`
3. Copy the built DLLs into that folder
4. Copy `command.json` (from repo root) into `Commands/RevitMCPCommandSet/`
5. Create (or update) `Commands/commandRegistry.json` as the runtime registry:
   - Each entry's `assemblyPath` must use the `{VERSION}` placeholder, e.g. `"RevitMCPCommandSet/{VERSION}/RevitMCPCommandSet.dll"`
   - The plugin replaces `{VERSION}` with the current Revit version at load time (e.g. `2026`)
   - See `Commands/RevitMCPCommandSet/command.json` for the full command definitions

## Supported Tools (84 Revit commands + 6 utilities)

### General

| Tool | Description |
| ---- | ----------- |
| `say_hello` | Display a greeting dialog in Revit (connection test) |
| `send_code_to_revit` | Send C# code to Revit to execute via Roslyn |
| `save_document` | Save the current Revit document |

### Query & Selection (10)

| Tool | Description |
| ---- | ----------- |
| `get_current_view_info` | Get current active view info (name, type, scale, detail level) |
| `get_current_view_elements` | Get elements from the current active view |
| `get_selected_elements` | Get currently selected elements |
| `get_available_family_types` | Get available family types in current project |
| `ai_element_filter` | Intelligent element querying tool with multiple filter criteria |
| `query_parameters` | Get all parameters of an element with name, value, and storage type |
| `query_geometry` | Get geometry of an element including bounding box, solids, and faces |
| `query_references` | Get stable geometric references for dimensioning and tagging |
| `check_interferences` | Check interference collisions between specified elements |
| `query_view_range` | Get the view range of a plan view |

### Create — Architecture (19)

| Tool | Description |
| ---- | ----------- |
| `create_wall` | Create walls with start/end points, height, thickness, and type |
| `create_floor` | Create floors with boundary polygon, thickness, and level |
| `create_ceiling` | Create ceilings with boundary, level, and thickness |
| `create_roof` | Create roofs with type (flat/pitched), boundary, and slope |
| `create_column` | Create architectural or structural columns at specified locations |
| `create_stair` | Create stairs with base/top level, width, riser, tread, and landings |
| `create_ramp` | Create ramps with base/top level and width |
| `create_railing` | Create railings along a path with height and type |
| `create_opening` | Create openings in walls, floors, or shafts |
| `create_model_curve` | Create model lines between start and end points |
| `create_reference_plane` | Create reference planes with start/end and normal direction |
| `create_group` | Create a group from selected element IDs |
| `create_grid` | Create a grid system with smart spacing generation |
| `create_level` | Create levels at specified elevations |
| `create_room` | Create and place rooms at specified locations |
| `create_structural_framing_system` | Create a structural beam framing system |
| `create_line_based_element` | Create line-based elements (wall, beam, pipe) — generic |
| `create_point_based_element` | Create point-based elements (door, window, furniture) — generic |
| `create_surface_based_element` | Create surface-based elements (floor, ceiling, roof) — generic |

### Create — MEP (10)

| Tool | Description |
| ---- | ----------- |
| `create_duct` | Create ducts with start/end points, width, height, and system type |
| `create_pipe` | Create pipes with start/end points, diameter, and system type |
| `create_conduit` | Create conduits with start/end points and diameter |
| `create_equipment` | Place MEP equipment at specified locations with rotation |
| `create_space` | Create MEP spaces at specified locations |
| `create_direct_shape` | Create primitive solid geometry (box, cylinder, extrusion) as DirectShape |
| `create_swept_shape` | Create swept solids along a path with section profiles |
| `create_mep_curve` | Create MEP curve elements (duct/pipe/conduit) — multi-type |
| `connect_mep` | Connect two MEP elements by their connectors |
| `create_mep_system` | Create MEP systems from selected elements |

### Annotation (8)

| Tool | Description |
| ---- | ----------- |
| `create_dimensions` | Create dimension annotations between elements or points |
| `create_text_note` | Create text notes in views with content, position, and alignment |
| `create_tag` | Create independent tags for elements (doors, windows, walls, rooms) |
| `tag_all_walls` | Tag all walls in the current view |
| `tag_all_rooms` | Tag all rooms in the current view |
| `create_filled_region` | Create a filled region in a view with boundary points |
| `create_revision` | Create a revision record with name, date, and number |
| `create_revision_cloud` | Create a revision cloud in a view associated with a revision |

### Views & Sheets (18)

| Tool | Description |
| ---- | ----------- |
| `create_view` | Create views (floor plan, ceiling plan, elevation, section, 3D) |
| `create_drafting_view` | Create a drafting view with specified name and scale |
| `create_section_view` | Create a section view with bounding box |
| `create_elevation_view` | Create an elevation view at a direction index |
| `create_callout` | Create a callout view from a host view |
| `duplicate_view` | Duplicate a view with duplicate, with detailing, or dependent mode |
| `create_view_template` | Create a view template from a source view |
| `create_sheet` | Create sheets with number, name, and optional title block |
| `place_view_on_sheet` | Place a view onto a sheet at a specified location |
| `create_schedule` | Create schedules (regular, material, keynote, view/sheet/revision list) |
| `place_schedule_on_sheet` | Place an existing schedule on a sheet |
| `create_detail_curve` | Create detail lines in a view |
| `set_view_properties` | Set view properties (scale, detail level, crop box, display style, template) |
| `set_category_overrides` | Set graphic overrides for a category in a view |
| `manage_view_filters` | Add or remove view filters with visibility and overrides |
| `set_view_range` | Set the plan view range offsets |
| `manage_schedule_fields` | Add, remove, reorder, or hide schedule fields |
| `manage_graphics_resources` | Manage line styles and fill patterns |

### Modify (10)

| Tool | Description |
| ---- | ----------- |
| `operate_element` | Operate on elements (select, setColor, hide, isolate, etc.) |
| `color_elements` | Color elements based on a parameter value |
| `delete_element` | Delete elements by ID |
| `set_parameters` | Batch set parameters on elements with key-value pairs |
| `transform_elements` | Move, copy, rotate, or mirror elements |
| `rename_element` | Rename a Revit element (level, grid, view, type) |
| `set_element_curve` | Modify location curve of linear elements |
| `duplicate_type` | Duplicate an element type with a new name |
| `manage_family_parameters` | Add, rename, remove, or set formulas on family parameters |
| `manage_project_parameters` | List or add shared parameters to the project |

### Family (2)

| Tool | Description |
| ---- | ----------- |
| `load_family` | Load a family .rfa file into the current project |
| `place_family_instance` | Place family instances (unhosted, hosted, face-based, workplane-based) |

### Analysis & Data (4)

| Tool | Description |
| ---- | ----------- |
| `analyze_model_statistics` | Analyze model complexity with element counts by category, type, family, and level |
| `export_room_data` | Export all room data from the project |
| `get_material_quantities` | Calculate material quantities and takeoffs |
| `export_views` | Export views to files (PNG, JPG, DWG, DXF, IFC) |

### Local Database (3)

| Tool | Description |
| ---- | ----------- |
| `store_project_data` | Store project metadata in local database |
| `store_room_data` | Store room metadata in local database |
| `query_stored_data` | Query stored project and room data |

## Testing

The test project uses [Nice3point.TUnit.Revit](https://github.com/Nice3point/RevitUnit) to run integration tests against a live Revit instance. No separate addin installation is required — the framework injects into the running Revit process automatically.

### Prerequisites

- **.NET 10 SDK** — required by Nice3point.Revit.Sdk 6.1.0. Install via `winget install Microsoft.DotNet.SDK.10`
- **Autodesk Revit 2026** (or 2025) — must be installed and licensed on your machine

### Running Tests

1. Open Revit 2026 (or 2025) and wait for it to fully load
2. Run the tests from the command line:

```bash
# For Revit 2026
dotnet test -c Debug.R26 -r win-x64 tests/commandset

# For Revit 2025
dotnet test -c Debug.R25 -r win-x64 tests/commandset
```

> **Note:** The `-r win-x64` flag is required on ARM64 machines because the Revit API assemblies are x64-only.

Alternatively, you can use `dotnet run`:

```bash
cd tests/commandset
dotnet run -c Debug.R26
```

### IDE Support

- **JetBrains Rider** — enable "Testing Platform support" in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform
- **Visual Studio** — tests should be discoverable through the standard Test Explorer

### Test Structure

| Directory | Purpose |
|-----------|---------|
| `tests/commandset/AssemblyInfo.cs` | Global `[assembly: TestExecutor<RevitThreadExecutor>]` registration |
| `tests/commandset/Architecture/` | Tests for level and room creation commands |
| `tests/commandset/DataExtraction/` | Tests for model statistics, room data export, and material quantities |
| `tests/commandset/ColorSplashTests.cs` | Tests for color override functionality |
| `tests/commandset/TagRoomsTests.cs` | Tests for room tagging functionality |

### Writing New Tests

Test classes inherit from `RevitApiTest` and use TUnit's async assertion API:

```csharp
public class MyTests : RevitApiTest
{
    private static Document _doc;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task MyTest_Condition_ExpectedResult()
    {
        var elements = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .ToElements();

        await Assert.That(elements.Count).IsGreaterThan(0);
    }
}
```

## Development

### MCP Server

```bash
cd server
npm install
npm run build
```

The server compiles TypeScript to `server/build/`. During development you can run it directly with `npx tsx server/src/index.ts`.

> **Note:** `better-sqlite3` requires native compilation (Python + C++ build tools). If `npm install` fails, you may need to install Python first or use the npm published package instead.

### Revit Plugin + Command Set

Open `mcp-servers-for-revit.sln` in Visual Studio. The solution contains both the plugin and command set projects. Build configurations target Revit 2020-2026:

- **Revit 2020-2024**: .NET Framework 4.8 (`Release R20` through `Release R24`)
- **Revit 2025-2026**: .NET 8 (`Release R25`, `Release R26`)

Building the solution automatically assembles the complete deployable layout in `plugin/bin/AddIn <year> <config>/` - the command set is copied into the plugin's `Commands/` folder as part of the build.

## Project Structure

```
mcp-servers-for-revit/
├── mcp-servers-for-revit.sln    # Combined solution (plugin + commandset + tests)
├── command.json     # Command set manifest
├── server/          # MCP server (TypeScript) - tools exposed to AI clients
├── plugin/          # Revit add-in (C#) - WebSocket bridge inside Revit
├── commandset/      # Command implementations (C#) - Revit API operations
├── tests/           # Integration tests (C#) - TUnit tests against live Revit
├── assets/          # Images for documentation
├── .github/         # CI/CD workflows, contributing guide, code of conduct
├── LICENSE
└── README.md
```

## Releasing

A single `v*` tag drives the entire release. The [release workflow](.github/workflows/release.yml) automatically:

- Builds the Revit plugin + command set for Revit 2020-2026
- Creates a GitHub release with `mcp-servers-for-revit-vX.Y.Z-Revit<year>.zip` assets
- Publishes the MCP server to npm as [`mcp-server-for-revit`](https://www.npmjs.com/package/mcp-server-for-revit)

To create a release:

1. Run the bump script (updates `server/package.json`, `server/package-lock.json`, and `plugin/Properties/AssemblyInfo.cs`, then commits and tags):
   ```powershell
   ./scripts/release.ps1 -Version X.Y.Z
   ```

2. Push to trigger the workflow:
   ```bash
   git push origin main --tags
   ```

> [!NOTE]
> npm publish uses [trusted publishing](https://docs.npmjs.com/trusted-publishers/) via OIDC — no npm token is required. Provenance attestation is generated automatically.

## Acknowledgements

This project is a fork of the work by the [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit) team. The original repositories:

- [revit-mcp](https://github.com/mcp-servers-for-revit/revit-mcp) - MCP server
- [revit-mcp-plugin](https://github.com/mcp-servers-for-revit/revit-mcp-plugin) - Revit plugin
- [revit-mcp-commandset](https://github.com/mcp-servers-for-revit/revit-mcp-commandset) - Command set

Thank you to the original authors for creating the foundation that this project builds upon.

## License

[MIT](LICENSE)
