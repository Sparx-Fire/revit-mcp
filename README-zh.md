# mcp-servers-for-revit

**通过模型上下文协议（MCP）将 AI 助手连接到 Autodesk Revit。**

mcp-servers-for-revit 使 Claude、Cline 等 AI 客户端能够读取、创建、修改和删除 Revit 项目中的图元。它由三个组件组成：TypeScript MCP 服务器（向 AI 暴露工具）、C# Revit 插件（桥接命令到 Revit）以及命令集（实现实际的 Revit API 操作）。

## 架构

```
MCP 客户端 (Claude, Cline 等)
    ↕ stdio
MCP 服务器 (TypeScript) — server/
    ↕ TCP/JSON-RPC (端口 8080)
Revit 插件 (C#) — plugin/
    → 加载
命令集 (C#) — commandset/
    → 执行
Revit API
```

> 注意：MCP 服务器与 Revit 插件之间使用 **原始 TCP Socket** 通信，基于 JSON-RPC 2.0 协议（非 WebSocket）。

## 系统要求

- **Autodesk Revit 2020–2026**
- **Node.js 18+**（仅源码编译时需要）

## 安装

### 方式一：ZIP 包安装（推荐）

从 [Releases](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit/releases) 下载对应 Revit 版本的 ZIP 包：

```
解压 ZIP → 右键 install.ps1 → 使用 PowerShell 运行
```

包内已包含 Node.js 运行时，**无需单独安装 Node.js**。

### 方式二：手动安装

解压 ZIP，复制到 Revit 插件目录：
```
%AppData%\Autodesk\Revit\Addins\<你的 Revit 版本>\
```

目录结构：
```
Addins/2026/
├── mcp-servers-for-revit.addin
└── revit_mcp_plugin/
    ├── RevitMCPPlugin.dll
    ├── runtime/           ← 内置 Node.js + MCP 服务器
    │   ├── node.exe
    │   └── build/index.js
    └── Commands/
        └── RevitMCPCommandSet/
            ├── command.json
            └── 2026/
                └── RevitMCPCommandSet.dll
```

### 方式三：源码编译安装

```powershell
.\build.ps1 -RevitVersion R26     # 编译命令集 + 打包运行时
.\install.ps1                      # 部署到 %AppData%
```

如只改了 C# 代码，跳过服务器编译：
```powershell
.\build.ps1 -RevitVersion R26 -SkipServer
```

> 编译后需要确保 `Commands/commandRegistry.json` 中的 `assemblyPath` 包含 `{VERSION}` 占位符（如 `RevitMCPCommandSet/{VERSION}/RevitMCPCommandSet.dll`），插件加载时会自动替换为当前 Revit 版本号。

### 配置 MCP 服务器

安装后，AI 客户端配置为使用内置运行时（无需独立安装 Node.js 或 npm）：

**Claude Code：**
```bash
claude mcp add mcp-server-for-revit -- "%APPDATA%/Autodesk/Revit/Addins/2026/revit_mcp_plugin/runtime/node.exe" "%APPDATA%/Autodesk/Revit/Addins/2026/revit_mcp_plugin/runtime/build/index.js"
```

**Claude Desktop：**
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

> 将 `2026` 替换为你实际的 Revit 版本年份。

### 启动服务

1. 启动 Revit，如提示未知加载项，点击 **始终加载**
2. 插件会自动启动 TCP 服务在 8080 端口，无需手动操作
3. 通过 MCP 客户端工具验证连接（如 `say_hello`）

### 3. 启动 Revit

启动 Revit，如果提示未知的加载项，点击 **始终加载**。在 mcp-servers-for-revit 功能区的 **Settings** 中启用所需命令并保存。

## 支持的工具（84 个 Revit 命令 + 6 个实用工具）

### 通用

| 工具 | 说明 |
| ---- | ---- |
| `say_hello` | 在 Revit 中显示对话框（连接测试） |
| `send_code_to_revit` | 通过 Roslyn 执行 C# 代码 |
| `save_document` | 保存当前 Revit 文档 |

### 查询与选择（10 个）

| 工具 | 说明 |
| ---- | ---- |
| `get_current_view_info` | 获取当前视图信息（名称、类型、比例、详细程度） |
| `get_current_view_elements` | 获取当前视图中的图元 |
| `get_selected_elements` | 获取当前选中的图元 |
| `get_available_family_types` | 获取项目中可用的族类型 |
| `ai_element_filter` | 智能图元查询（多条件过滤） |
| `query_parameters` | 获取图元的所有参数（名称、值、类型） |
| `query_geometry` | 获取图元几何信息（包围盒、实体、面） |
| `query_references` | 获取几何参照（用于标注和标记） |
| `check_interferences` | 检查图元之间的碰撞干涉 |
| `query_view_range` | 获取平面视图的视图范围 |

### 创建 — 建筑（19 个）

| 工具 | 说明 |
| ---- | ---- |
| `create_wall` | 创建墙体（起点/终点、高度、厚度、类型） |
| `create_floor` | 创建楼板（边界多边形、厚度、标高） |
| `create_ceiling` | 创建天花（边界、标高、厚度） |
| `create_roof` | 创建屋顶（平顶/坡顶、边界、坡度） |
| `create_column` | 创建结构柱或建筑柱 |
| `create_stair` | 创建楼梯（梯段+平台、踏步高度/深度） |
| `create_ramp` | 创建坡道 |
| `create_railing` | 创建栏杆扶手 |
| `create_opening` | 创建洞口（墙/楼板/竖井） |
| `create_model_curve` | 创建模型线 |
| `create_reference_plane` | 创建参照平面 |
| `create_group` | 创建图元组 |
| `create_grid` | 创建轴网系统 |
| `create_level` | 创建标高 |
| `create_room` | 创建房间 |
| `create_structural_framing_system` | 创建结构梁系统 |
| `create_line_based_element` | 通用方式创建线状构件（墙/梁/管） |
| `create_point_based_element` | 通用方式创建点状构件（门/窗/家具） |
| `create_surface_based_element` | 通用方式创建面状构件（楼板/天花/屋顶） |

### 创建 — MEP 机电（10 个）

| 工具 | 说明 |
| ---- | ---- |
| `create_duct` | 创建风管（起点/终点、宽度、高度、系统类型） |
| `create_pipe` | 创建管道 |
| `create_conduit` | 创建线管 |
| `create_equipment` | 放置机电设备 |
| `create_space` | 创建 MEP 空间 |
| `create_direct_shape` | 创建基本几何实体（DirectShape） |
| `create_swept_shape` | 创建扫掠实体（矩形/圆形/马蹄形截面） |
| `create_mep_curve` | 创建 MEP 管线（风管/管道/线管） |
| `connect_mep` | 连接两个 MEP 图元 |
| `create_mep_system` | 创建 MEP 系统 |

### 注释（8 个）

| 工具 | 说明 |
| ---- | ---- |
| `create_dimensions` | 创建尺寸标注 |
| `create_text_note` | 创建文字注释 |
| `create_tag` | 创建独立标记 |
| `tag_all_walls` | 标记所有墙 |
| `tag_all_rooms` | 标记所有房间 |
| `create_filled_region` | 创建填充区域 |
| `create_revision` | 创建修订 |
| `create_revision_cloud` | 创建修订云线 |

### 视图与图纸（18 个）

| 工具 | 说明 |
| ---- | ---- |
| `create_view` | 创建视图（平面/天花板/立面/剖面/3D） |
| `create_drafting_view` | 创建详图视图 |
| `create_section_view` | 创建剖面视图 |
| `create_elevation_view` | 创建立面视图 |
| `create_callout` | 创建局部视图 |
| `duplicate_view` | 复制视图 |
| `create_view_template` | 创建视图样板 |
| `create_sheet` | 创建图纸 |
| `place_view_on_sheet` | 在图纸上放置视图 |
| `create_schedule` | 创建明细表 |
| `place_schedule_on_sheet` | 在图纸上放置明细表 |
| `create_detail_curve` | 创建详图线 |
| `set_view_properties` | 设置视图属性（比例/详细程度/裁剪/显示/样板） |
| `set_category_overrides` | 设置类别图形替换 |
| `manage_view_filters` | 管理视图过滤器 |
| `set_view_range` | 设置平面视图范围 |
| `manage_schedule_fields` | 管理明细表字段 |
| `manage_graphics_resources` | 管理线样式和填充图案 |

### 编辑修改（10 个）

| 工具 | 说明 |
| ---- | ---- |
| `operate_element` | 图元操作（选择、颜色、隐藏、隔离等） |
| `color_elements` | 按参数值为图元着色 |
| `delete_element` | 删除图元 |
| `set_parameters` | 批量设置图元参数 |
| `transform_elements` | 移动/复制/旋转/镜像图元 |
| `rename_element` | 重命名图元 |
| `set_element_curve` | 修改线性图元的路径 |
| `duplicate_type` | 复制类型 |
| `manage_family_parameters` | 管理族参数（添加/重命名/删除/公式） |
| `manage_project_parameters` | 管理项目参数 |

### 族操作（2 个）

| 工具 | 说明 |
| ---- | ---- |
| `load_family` | 加载 .rfa 族文件 |
| `place_family_instance` | 放置族实例（多种放置方式） |

### 分析与数据（4 个）

| 工具 | 说明 |
| ---- | ---- |
| `analyze_model_statistics` | 分析模型复杂度 |
| `export_room_data` | 导出房间数据 |
| `get_material_quantities` | 计算材料用量 |
| `export_views` | 导出视图（PNG、JPG、DWG、DXF、IFC） |

### 本地数据库（3 个）

| 工具 | 说明 |
| ---- | ---- |
| `store_project_data` | 存储项目元数据到本地数据库 |
| `store_room_data` | 存储房间数据到本地数据库 |
| `query_stored_data` | 查询本地数据库 |

## 项目结构

```
mcp-servers-for-revit/
├── command.json          # 命令清单
├── server/               # MCP 服务器 (TypeScript)
├── plugin/               # Revit 插件 (C#)
├── commandset/           # 命令实现 (C#)
├── tests/                # 集成测试 (C#)
├── assets/               # 文档图片
├── scripts/              # 构建脚本
├── .github/              # CI/CD 工作流
└── README.md
```

## 开发

### MCP 服务器
```bash
cd server
npm install
npm run build
```

编译后的 JS 输出到 `server/build/`。开发时也可直接运行源码：
```bash
npx tsx server/src/index.ts
```

> 注意：`better-sqlite3` 需要本地编译（依赖 Python + C++ 工具链）。如果 `npm install` 失败，可先安装 Python 或改用 npm 发布版。

### Revit 插件 + 命令集
用 Visual Studio 打开 `mcp-servers-for-revit.sln`，选择对应 Revit 版本的配置进行编译。

## 测试
```bash
# Revit 2026
dotnet test -c Debug.R26 -r win-x64 tests/commandset

# Revit 2025
dotnet test -c Debug.R25 -r win-x64 tests/commandset
```

## 许可证

[MIT](LICENSE)
