# mcp-servers-for-revit 改造方案

## 0. 术语说明

以 `revit-mcp-local-bridge` 的 **65 个原子操作** 为基准目标。改造工作按实现成本分三类：

| 类别 | 含义 | 需要写什么 |
|:----:|------|:---------:|
| ✅ 不动 | 已有对应命令，无需修改 | 0 |
| 🔧 Model 有 → 只写命令 | `Models/` 下已有 DTO 类 | Command + EventHandler + TS Tool |
| 🆕 都写 | 无 Model 也无命令 | Model + Command + EventHandler + TS Tool |

> 每个命令的完整链路：TS Tool → TCP/JSON-RPC → C# Command → C# EventHandler → Revit API。Model 只定义了数据结构，不参与执行。

---

## 1. ✅ 已有、不动（12 个）

| bridge 操作 | mcp 已有命令 | 备注 |
|-------------|-------------|------|
| `health` | `say_hello` | 连接验证 |
| `query_document`（基本信息） | `get_current_view_info` | 视图名称/类型/比例/详细程度 |
| `query_selection` | `get_selected_elements` | 选中图元 ID/名称/类别 |
| `query_elements`（当前视图列表） | `get_current_view_elements` | 按类别/可见性过滤 |
| `query_elements`（条件过滤） | `ai_element_filter` | BuiltInCategory/类型/族/包围盒 |
| `query_catalog`（族类型部分） | `get_available_family_types` | 按类别/族名过滤 |
| `create_level` | `create_level` | 等价 |
| `create_grid` | `create_grid` | 等价 |
| `create_room` | `create_room` | 等价 |
| `create_dimension` | `create_dimensions` | 等价 |
| `delete_elements` | `delete_element` | 等价 |
| `select_elements` | `operate_element (select)` | operate_element 的 Select 模式 |
| `set_element_overrides`（颜色/隐藏） | `color_elements` + `operate_element` | Hide/TempHide/Isolate/SetColor |
| `query_room` | `export_room_data` | 提取房间明细 |
| `analyze_model_statistics` | `analyze_model_statistics` | 等价 |
| `get_material_quantities` | `get_material_quantities` | 等价 |

> *注：`analyze_model_statistics` 和 `get_material_quantities` 在 bridge 中没有直接原子操作，但 mcp 已有实现，是增强能力。*

---

## 2. 🔧 Model 已有，需写命令（22 个）

**Models/ 下已有 DTO，只需补 Command + EventHandler + TS Tool 三层。**

### 2.1 Architecture 建筑（12 个）

| 操作 | 已有 Model | 说明 |
|------|-----------|------|
| `create_wall` | `WallCreationInfo` | 现有 `create_line_based_element` 的路由分支可拆出 |
| `create_floor` | `FloorInfo` | 现有 `create_surface_based_element` 的路由分支可拆出 |
| `create_ceiling` | `CeilingCreationInfo` | 同上 |
| `create_roof` | `RoofInfo` | 同上 |
| `create_column` | `ColumnInfo` | 独立创建结构柱/建筑柱 |
| `create_stair` | `StairCreationInfo` | 含梯段+平台 |
| `create_ramp` | `RampCreationInfo` | 坡道 |
| `create_railing` | `RailingCreationInfo` | 栏杆扶手 |
| `create_opening` | `OpeningCreationInfo` | 墙洞口/楼板洞口/竖井洞口 |
| `create_model_curve` | `ModelCurveCreationInfo` | 模型线 |
| `create_reference_plane` | `ReferencePlaneCreationInfo` | 参照平面 |
| `create_group` | `GroupCreationInfo` | 图元成组（bridge 无此操作，额外能力） |

### 2.2 MEP 机电（4 个）

| 操作 | 已有 Model | 说明 |
|------|-----------|------|
| `create_duct` | `DuctCreationInfo` | 现有 `create_line_based_element` 的 duct 分支可拆出 |
| `create_pipe` | `PipeCreationInfo` | 管道 |
| `create_conduit` | `ConduitCreationInfo` | 线管 |
| `create_equipment` | `EquipmentCreationInfo` | 机电设备放置 |

### 2.3 Views 视图与图纸（4 个）

| 操作 | 已有 Model | 说明 |
|------|-----------|------|
| `create_view` | `ViewCreationInfo` | 3D/平面/天花板/立面/剖面 |
| `create_sheet` | `SheetCreationInfo` | 图纸 |
| `place_view_on_sheet` | `ViewportCreationInfo` | 视图布图 |
| `create_schedule` | `ScheduleCreationInfo` | 明细表 |

### 2.4 Annotation 注释（2 个）

| 操作 | 已有 Model | 说明 |
|------|-----------|------|
| `create_text_note` | `TextNoteCreationInfo` | 文字注释 |
| `create_tag` | `TagCreationInfo` | 独立标记（目前只有 tag_all_rooms/walls） |

### 文件布局

```
commandset/Commands/Architecture/     CreateWallCommand.cs     (new)
                                       CreateFloorCommand.cs    (new)
                                       CreateCeilingCommand.cs  (new)
                                       CreateRoofCommand.cs     (new)
                                       CreateColumnCommand.cs   (new)
                                       CreateStairCommand.cs    (new)
                                       CreateRampCommand.cs     (new)
                                       CreateRailingCommand.cs  (new)
                                       CreateOpeningCommand.cs  (new)
                                       CreateModelCurveCommand.cs (new)
                                       CreateReferencePlaneCommand.cs (new)
                                       CreateGroupCommand.cs    (new)
commandset/Commands/MEP/              CreateDuctCommand.cs      (new)
                                       CreatePipeCommand.cs      (new)
                                       CreateConduitCommand.cs   (new)
                                       CreateEquipmentCommand.cs (new)
commandset/Commands/Views/            CreateViewCommand.cs      (new)
                                       CreateSheetCommand.cs     (new)
                                       PlaceViewOnSheetCommand.cs (new)
                                       CreateScheduleCommand.cs  (new)
commandset/Commands/Annotation/       CreateTextNoteCommand.cs  (new)
                                       CreateTagCommand.cs       (new)

commandset/Services/Architecture/     ...EventHandler.cs ×12   (new)
commandset/Services/MEP/              ...EventHandler.cs ×4    (new)
commandset/Services/Views/            ...EventHandler.cs ×4    (new)
commandset/Services/Annotation/       ...EventHandler.cs ×2    (new)

server/src/tools/                     create_wall.ts           (new)
                                       ... 共 22 个 .ts 文件    (new)
```

**估算：** 每个 ~80 行 C# + ~50 行 TS = ~130 行/个，22 个约 **2900 行**，**3~4 天**。

---

## 3. 🆕 Model + 命令都要写（31 个）

### 3.1 需要新建 Model 的（6 个）

这些操作输入数据结构复杂，需要独立 DTO：

| 操作 | 要新建的 Model | 关键字段 |
|------|---------------|---------|
| `create_direct_shape` | `DirectShapeCreationInfo` | ElementType, Box/Cylinder/Extrusion 参数, Category |
| `create_swept_shape` | `SweptShapeCreationInfo` | Path curve, Section shape (rect/circle/horseshoe), Section params |
| `create_space` | `SpaceCreationInfo` | Level, Point, Name, Number |
| `connect_mep` | `MEPConnectInfo` | ElementId1, ElementId2, ConnectType (elbow/tee/reducer), ConnectorIndex |
| `create_mep_system` | `MEPSystemCreationInfo` | SystemType, Elements, Name |
| `place_family_instance` | `FamilyInstancePlacementInfo` | Symbol/TypeId, Placement method (unhosted/hosted/face/workplane), Location, HostId |
| `export_views` | `ExportSettingsInfo` | ViewIds, Format (png/dwg/ifc), Path, Options |

### 3.2 无需 Model、直接 JObject/基本类型传参（25 个）

这些操作输入简单，Command 内直接解析 `JObject` 即可：

**查询增强（5 个）：**

| 操作 | 输入 | 实现逻辑 |
|------|------|---------|
| `query_parameters` | `elementId` | 遍历 `Element.Parameters` 返回名称/值/类型/单位 |
| `query_geometry` | `elementId` | `Element.get_Geometry(opts)` → 包围盒/面/体 |
| `query_references` | `elementId` | 稳定几何参照（用于标注） |
| `check_interferences` | `elementIds[]` | `ElementIntersectsFilter` 或 `IntersectionResult` |
| `query_view_range` | `viewId` | 平面视图的 top/cut plane/bottom/depth |

**创建（8 个）：**

| 操作 | 输入 | 实现逻辑 |
|------|------|---------|
| `create_drafting_view` | `name, scale, vftName` | `ViewDrafting.Create(doc, vftId)` |
| `create_section_view` | `name, boundingBox, vftName` | `ViewSection.Create(doc, vftId, paramElementId)` |
| `create_elevation_view` | `name, direction, vftName` | `ViewSection.Create(doc, vftId, paramElementId)` |
| `create_callout` | `name, hostViewId, boundingBox` | `ViewSection.CreateCallout(doc, ...)` |
| `duplicate_view` | `viewId, mode` | `view.Duplicate(mode)` |
| `create_view_template` | `sourceViewId, name` | `View.CreateViewTemplate(doc, viewId)` |
| `create_detail_curve` | `curve, viewId` | `Doc.Create.NewDetailCurve(view, curve)` |
| `create_filled_region` | `curveArray, typeName, viewId` | `FilledRegion.Create(doc, typeId, viewId, curves)` |
| `create_revision` | `name, date, num` | `Revision.Create(doc)` + set params |
| `create_revision_cloud` | `revisionId, curveArray, viewId` | `RevisionCloud.Create(doc, revisionId, curves, viewId)` |
| `place_schedule_on_sheet` | `scheduleId, sheetId, point` | `ScheduleSheetInstance.Create(doc, sheetId, scheduleId, point)` |
| `load_family` | `filePath` | `Doc.LoadFamily(path, options)` |
| `create_mep_curve` | `type, start, end, level, systemType` | 统一 MEP 线状创建（路由到 Duct/Pipe/Conduit） |

**视图属性与覆盖（7 个）：**

| 操作 | 输入 | 实现逻辑 |
|------|------|---------|
| `set_view_properties` | `viewId, props{}` | 批量设置 View 属性（scale/crop/detail/template） |
| `set_category_overrides` | `viewId, categoryId, override{}` | `View.SetCategoryOverrides(catId, ov)` |
| `manage_view_filters` | `viewId, action, filterId, rule, override` | 添加/移除视图过滤器 |
| `set_view_range` | `viewId, top, cut, bottom, depth` | `ViewPlan.ViewRange` |
| `manage_schedule_fields` | `scheduleId, action, fieldName, opts` | 添加/删除/排序/隐藏明细表字段 |
| `manage_graphics_resources` | `action, name, properties` | 线样式/填充图案管理 |
| `create_revision_cloud` | 已在上面列出 | — |

**编辑修改（4 个）：**

| 操作 | 输入 | 实现逻辑 |
|------|------|---------|
| `set_parameters` | `elementId, params{}` | `Element.get_Parameter(name).Set(value)` 批量 |
| `transform_elements` | `elementIds[], transformType, params` | `ElementTransformUtils.Move/Copy/Rotate/Mirror` |
| `rename_element` | `elementId, newName` | `Parameter.Set` (Element.Name 属性的参数) |
| `set_element_curve` | `elementId, newCurve` | `LocationCurve.Curve = newCurve` |
| `duplicate_type` | `typeId, newName` | `ElementType.Duplicate(name)` |
| `manage_family_parameters` | `action, familyId, name, formula` | `FamilyManager.AddParameter/DeleteParameter` |
| `manage_project_parameters` | `action, paramGroupName` | 共享参数管理 |

**导出/保存（1 个）：**

| 操作 | 输入 | 实现逻辑 |
|------|------|---------|
| `save_document` | (无) | `Doc.Save()` |
| `export_views` | 见 3.1 需要 Model | 含格式选项 |

---

## 4. 完整改造清单汇总

| 类别 | 数量 | 工作量 | 工期估 |
|:----:|:----:|--------|:-----:|
| ✅ 不动 | 12 | 0 | — |
| 🔧 有 Model 写命令 | 22 | Command+EventHandler+TS Tool ×22 | 3~4 天 |
| 🆕 新建 Model+命令 | 6 | Model + 全套 ×6 | 1~2 天 |
| 🆕 无需 Model 仅写命令 | 25 | Command+EventHandler+TS Tool ×25 | 2~3 天 |
| **总计** | **65** | 需实现 53 个新命令 | **6~9 天** |

### 推荐执行顺序

```
第 1 周 ─── 🔧 22 个有 Model 的命令（成本最低，用完现有资产）
               ├── Architecture ×12
               ├── MEP ×4
               ├── Views ×4
               └── Annotation ×2

第 2 周 ─── 🆕 25 个无需 Model 的命令（逻辑简单，无数据依赖）
               ├── 查询增强 ×5
               ├── 创建操作 ×13
               ├── 视图属性 ×7
               └── 编辑修改 ×4 + 保存

第 3 周 ─── 🆕 6 个需要新 Model 的命令（设计+实现）
               ├── DirectShape / SweptShape / Space
               ├── MEP Connect / System / FamilyInstance
               └── Export
```

### 关键注意点

1. **不要删除已有的通用命令**：`create_line_based_element` / `create_surface_based_element` / `create_point_based_element` 保留了"一条命令创建多种构件"的灵活性，新增独立命令是更专业的特化接口。
2. **TS 侧自动注册**：`server/src/tools/register.ts` 会自动扫描所有导出了 `register*` 函数的 `.ts` 文件，只需新建文件即可，无需手动注册。
3. **command.json 需要逐条添加**：每新增一个 C# Command 都要在 `command.json` 中添加一条 `{ commandName, description, assemblyPath }`。
4. **超时设置**：重型操作（`create_stair`、`duplicate_view`、`export_views`、`create_swept_shape`）应将 `RaiseAndWaitForCompletion` 超时设为 30000ms 以上。
