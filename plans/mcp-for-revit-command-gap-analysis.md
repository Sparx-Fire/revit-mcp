# mcp-servers-for-revit 缺失命令集分析报告

## 一、现状概览

### 1.1 工程架构

```
AI Client → TS MCP Server (server/) → TCP/JSON-RPC → Revit AddIn (plugin/) → CommandSet DLL (commandset/) → Revit API
```

每一条命令需要 **4 层**完整实现：

| 层级 | 路径 | 职责 |
|------|------|------|
| TS Tool | `server/src/tools/xxx.ts` | MCP 工具注册、参数 Schema、连接 Revit |
| C# Command | `commandset/Commands/xxx.cs` | JSON-RPC 参数解析、调用 EventHandler |
| C# EventHandler | `commandset/Services/xxx.cs` | 实际 Revit API 操作、Transaction 管理 |
| Config | `command.json` | 注册命令名称到 DLL |

### 1.2 当前已实现命令（23 条）

| 类别 | 已实现命令 |
|------|-----------|
| **Test** | say_hello |
| **查询** | get_current_view_info, get_current_view_elements, get_selected_elements, get_available_family_types, ai_element_filter |
| **创建-通用** | create_line_based_element, create_point_based_element, create_surface_based_element |
| **创建-专业** | create_grid, create_level, create_room, create_structural_framing_system |
| **注释** | create_dimensions, tag_all_rooms, tag_all_walls |
| **修改** | operate_element, delete_element, color_elements |
| **分析** | analyze_model_statistics, export_room_data, get_material_quantities |
| **执行** | send_code_to_revit |

### 1.3 已定义 Model 类但未实现命令（37 个 DTO）

这些 `Models/` 下的 DTO 类已被创建，但对应的 Command + EventHandler + TS Tool 缺失：

| 类别 | 现存 Model | 数量 |
|------|-----------|------|
| **Architecture** | WallCreationInfo, FloorInfo, CeilingCreationInfo, RoofInfo, ColumnInfo, BeamInfo, StairCreationInfo, RampCreationInfo, RailingCreationInfo, OpeningCreationInfo, ShaftCreationInfo, FoundationInfo, AreaCreationInfo, ZoneCreationInfo, BalconyInfo, BuildingInfo, GroupCreationInfo, ModelCurveCreationInfo, ReferencePlaneCreationInfo, DoorInfo, WindowInfo, GridInfo, LevelInfo, RoomCreationInfo | 25 |
| **MEP** | DuctCreationInfo, PipeCreationInfo, ConduitCreationInfo, EquipmentCreationInfo | 4 |
| **Views** | ViewCreationInfo, SheetCreationInfo, SheetInfo, ViewportCreationInfo, ScheduleCreationInfo | 5 |
| **Annotation** | TextNoteCreationInfo, TagCreationInfo (DimensionCreationInfo 已实现) | 3 |

---

## 二、与 `revit-mcp-local-bridge` 的对比

`revit-mcp-local-bridge` 是一个成熟参考系，实现了 **65 个原子操作**。以它为基准，`mcp-servers-for-revit` 的差距如下：

| 能力域 | bridge 操作数 | mcp-servers 实现数 | 差距 |
|--------|:-----------:|:-----------------:|:----:|
| 查询 (Query) | 11 | 5 | -6 |
| 创建-建筑 (Create-Arch) | 10+ | 4 (通用) | -10+ |
| 创建-MEP | 6 | 0 | -6 |
| 创建-视图/图纸 | 15 | 0 | -15 |
| 注释标注 | 5 | 3 | -2 |
| 视图属性/覆盖 | 7 | 0 | -7 |
| 编辑/修改 | 10 | 3 | -7 |
| 图元/族管理 | 4 | 0 | -4 |
| 输出/导出 | 2 | 0 | -2 |
| **合计** | **~65** | **23** | **-42** |

---

## 三、缺失命令完整清单（42 条）

### 优先级 P0 — 核心建筑构件创建（10 条）

这些是 Revit 建模最常用的命令，用现有 model 即可直接实现：

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `create_wall` | ✅ WallCreationInfo | 独立墙体创建（目前通过 line_based 通用方式） |
| `create_floor` | ✅ FloorInfo | 楼板创建（支持边界+洞口+坡度） |
| `create_ceiling` | ✅ CeilingCreationInfo | 天花创建 |
| `create_roof` | ✅ RoofInfo | 屋顶创建（平顶/坡顶） |
| `create_column` | ✅ ColumnInfo | 结构柱/建筑柱 |
| `create_stair` | ✅ StairCreationInfo | 楼梯创建（含梯段+平台） |
| `create_ramp` | ✅ RampCreationInfo | 坡道创建 |
| `create_railing` | ✅ RailingCreationInfo | 栏杆扶手创建 |
| `create_opening` | ✅ OpeningCreationInfo | 洞口（墙/楼板/竖井） |
| `create_model_curve` | ✅ ModelCurveCreationInfo | 模型线 |

### 优先级 P1 — MEP 机电创建（6 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `create_duct` | ✅ DuctCreationInfo | 风管创建 |
| `create_pipe` | ✅ PipeCreationInfo | 管道创建 |
| `create_conduit` | ✅ ConduitCreationInfo | 线管创建 |
| `create_cable_tray` | ❌ 需要新建 | 桥架创建 |
| `create_equipment` | ✅ EquipmentCreationInfo | 机电设备放置 |
| `create_mep_curve` | — | MEP 统一点线创建 |

### 优先级 P2 — 视图与图纸（7 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `create_view` | ✅ ViewCreationInfo | 3D/平面/立面/剖面视图 |
| `create_sheet` | ✅ SheetCreationInfo | 图纸创建 |
| `place_view_on_sheet` | ✅ ViewportCreationInfo | 视图布图 |
| `create_drafting_view` | — | 详图视图 |
| `duplicate_view` | — | 视图复制 |
| `create_schedule` | ✅ ScheduleCreationInfo | 明细表创建 |
| `set_view_properties` | — | 视图属性（比例/裁剪/详细程度） |

### 优先级 P2 — 注释（3 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `create_text_note` | ✅ TextNoteCreationInfo | 文字注释 |
| `create_tag` | ✅ TagCreationInfo | 独立标记（不限于房间/墙） |
| `create_filled_region` | — | 填充区域 |

### 优先级 P3 — 编辑修改（7 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `set_parameters` | — | 批量设置图元参数 |
| `transform_elements` | — | 移动/复制/旋转/镜像 |
| `duplicate_type` | — | 复制族类型 |
| `rename_element` | — | 重命名 |
| `set_element_curve` | — | 修改线性图元路径 |
| `select_elements` | — | 从代码端选择图元 |
| `manage_family_parameters` | — | 族参数管理 |

### 优先级 P3 — 族操作（3 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `load_family` | — | 加载 .rfa |
| `place_family_instance` | — | 放置族实例（支持多种放置方式） |
| `list_family_templates` | — | 列出可用族模板 |

### 优先级 P3 — 增强查询（5 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `query_document` | — | 文档信息（项目名/路径/视图） |
| `query_catalog` | — | 项目资源目录（所有族/类型/视图） |
| `query_parameters` | — | 图元参数详情列表 |
| `query_geometry` | — | 几何查询（包围盒/面/体） |
| `check_interferences` | — | 碰撞检查 |

### 优先级 P4 — 导出/输出（1 条）

| 命令名 | Model 是否存在 | 说明 |
|--------|:------------:|------|
| `export_views` | — | 导出 PNG/JPG/DWG/IFC |

---

## 四、总计

| 优先级 | 条数 | 类型 |
|:------:|:----:|------|
| **P0** | 10 | 🏗️ 核心建筑构件创建（Model 已存在，仅需补齐 Command+EventHandler+TS Tool） |
| **P1** | 6 | 🔧 MEP 机电（4 条 Model 已存在） |
| **P2** | 10 | 📐 视图/图纸/注释（5 条 Model 已存在） |
| **P3** | 15 | 🛠️ 编辑/族/查询（Model 均不存在） |
| **P4** | 1 | 📤 导出 |
| **总计** | **42** | |

其中 **Model 已存在**的命令有 **19 条**，实现成本最低。
