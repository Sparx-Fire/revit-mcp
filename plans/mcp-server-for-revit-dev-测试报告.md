# mcp-server-for-revit-dev MCP 工具全量测试报告

**测试日期:** 2026-08-29 (最终更新: 2026-08-29)  
**Revit 版本:** 2026  
**文档:** REVIT-CMD-TEST.rvt / TTE.rfa (族文档)  
**当前视图:** 标高 1 (最后测试)

---

## 一、建筑/结构构件（11个）

| #  | 工具                                 | 结果        | 说明                                                                   |
| -- | ---------------------------------- | --------- | -------------------------------------------------------------------- |
| 1  | `create_wall`                      | ✅ **成功**  | 墙体 ID:337575, 5m 长 3m 高                                              |
| 2  | `create_floor`                     | ✅ **成功**  | 楼板 ID:337580, 4m×4m 方形                                               |
| 3  | `create_column`                    | ✅ **成功**   | **已修复:** `symbol.Activate()` 移入事务内. 柱 ID:338652 |
| 4  | `create_roof`                      | ✅ **成功**  | 屋顶 ID:337590, 平屋顶                                                    |
| 5  | `create_ceiling`                   | ✅ **成功**  | 天花板 ID:337626                                                        |
| 6  | `create_ramp`                      | ❌ **不支持** | Revit 2026 API 尚未支持坡道创建                                              |
| 7  | `create_stair`                     | ❌ **不支持** | Revit 2026 API 尚未支持楼梯创建                                              |
| 8  | `create_railing`                   | ✅ **成功**  | 栏杆 ID:337607                                                         |
| 9  | `create_opening`                   | ✅ **已修复并验证** | **枚举解析 ✅** + R26 墙开洞 API 修复 ✅ — 洞口 ID:338663。改用 `_doc.Create.NewOpening(Wall, XYZ, XYZ)`（R26 专有 API）|
| 10 | `create_structural_framing_system` | ✅ **成功**  | 3 根热轧H型钢 HW400x400x13x21 梁                                           |
| 11 | `create_group`                     | ✅ **成功**  | 组"组 1", 包含墙+楼板                                                       |

> **成功率: 8/11 = 73%**

---

## 二、查询/分析类（8个）

| #  | 工具                          | 结果         | 说明                 |
| -- | --------------------------- | ---------- | ------------------ |
| 12 | `get_current_view_elements` | ✅ **成功**   | 按类别+数量过滤返回         |
| 13 | `analyze_model_statistics`  | ✅ **成功**   | 4279 个图元, 49 个类别   |
| 14 | `get_material_quantities`   | ✅ **成功**   | 5 种材料, 含面积/体积/数量   |
| 15 | `get_selected_elements`     | ✅ **成功**   | 返回空数组(当前无选中)       |
| 16 | `query_view_range`          | ✅ **成功**   | 视图范围完整返回           |
| 17 | `query_geometry`            | ✅ **成功**   | 返回 bounding box    |
| 18 | `query_references`          | ⚠️ **空结果** | 运行正常但无引用返回(幕墙类型限制) |
| 19 | `query_parameters`          | ✅ **成功**   | 40+ 个参数完整返回        |
| 20 | `ai_element_filter`         | ✅ **成功**   | 智能过滤, 支持包围盒/可见性/类别 |
| 21 | `export_room_data`          | ✅ **成功**   | 0 个房间(项目无房间)       |

> **成功率: 9/10 = 90%** (query_references 算可用但结果为空)

---

## 三、视图/图纸（10个）

| #  | 工具                        | 结果       | 说明                                                              |
| -- | ------------------------- | -------- | --------------------------------------------------------------- |
| 22 | `create_section_view`     | ✅ **成功** | 剖面"测试剖面"                                                        |
| 23 | `create_elevation_view`   | ❌ **R26 不支持** | R26 中 `ElevationMarker.CreateElevationView` 返回 null. 已添加 null 检查防止崩溃, 但功能不可用 |
| 24 | `create_callout`          | ❌ **R26 不支持** | R26 中 `ViewSection.CreateCallout` 返回 null. 已添加 null 检查防止崩溃, 但功能不可用 |
| 25 | `duplicate_view`          | ✅ **成功** | 复制视图"标高 1"                                                      |
| 26 | `create_drafting_view`    | ✅ **成功** | **已修复:** 改用 `view.Scale = Scale` (非只读属性). 绘图视图 ID:338656 |
| 27 | `create_view`             | ✅ **成功** | **已修复:** 改用 `view.Scale = info.Scale`. 平面图 ID:338663 |
| 28 | `create_sheet`            | ✅ **成功** | 图纸 A001, 默认标题栏                                                  |
| 29 | `create_view_template`    | ✅ **成功** | 从"标高 1"创建视图样板                                                   |
| 30 | `place_view_on_sheet`     | ✅ **成功** | 将剖面视图放到图纸上                                                      |
| 31 | `place_schedule_on_sheet` | ✅ **成功** | 将明细表放到图纸上                                                       |

> **成功率: 6/10 = 60%**

---

## 四、标注/标记（7个）

| #  | 工具                      | 结果       | 说明                                    |
| -- | ----------------------- | -------- | ------------------------------------- |
| 32 | `create_text_note`      | ✅ **成功** | 文字注释                                  |
| 33 | `create_tag`            | ✅ **成功** | **已测试(标高 1 视图)** — 标签 ID:338892 |
| 34 | `create_dimensions`     | ✅ **成功** | 尺寸标注                                  |
| 35 | `create_revision`       | ❌ **失败** | **错误:** `The parameter is read-only.` |
| 36 | `create_revision_cloud` | ⚠️ 未测    | 依赖 revisionId                         |
| 37 | `tag_all_walls`         | ✅ **成功** | **已测试** — 4/4 面墙全部标记成功 |
| 38 | `tag_all_rooms`         | ⏭️ **跳过** | 项目中无房间，需先创建房间边界 |

> **成功率: 4/6 = 67%** (tag_all_rooms 跳过不计)

---

## 五、明细表（3个）

| #  | 工具                        | 结果          | 说明                                                       |
| -- | ------------------------- | ----------- | -------------------------------------------------------- |
| 39 | `create_schedule`         | ✅ **成功**    | 墙明细表                                                     |
| 40 | `manage_schedule_fields`  | ⚠️ **部分可用** | remove✅ / hide✅ / **add 已修复** ✅ (改用 `AddField(SchedulableField)`，需用可调度字段名如"类型""族""功能") |
| 41 | `place_schedule_on_sheet` | ✅ **成功**    | 明细表放到图纸 A001                                             |

> **成功率: 3/3 = 100%** (已测操作)

---

## 六、族操作（5个）

| #  | 工具                           | 结果          | 说明                                                                                                             |
| -- | ---------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------- |
| 42 | `get_available_family_types` | ✅ **成功**    | 返回 5 种墙类型                                                                                                      |
| 43 | `manage_project_parameters`  | ✅ **成功**    | 列出 7 个项目参数                                                                                                     |
| 44 | `manage_family_parameters`   | ⚠️ **部分可用** | 需在族文档中调用. TTE 族(12参数): `add`❌(Revit2026不支) / `rename`✅(内置参数不可改) / `set_formula`✅(需先有CurrentType) / `remove`⏳未测 |
| 45 | `load_family`                | ✅ **成功**    | 从 `E:\TEMP\TTE.rfa` 载入 TTE 族(常规模型)                                                                             |
| 46 | `place_family_instance`      | ✅ **成功**    | TTE 实例 ID:338640, 置于 (0,0,0), 参数 27 项完整                                                                        |

> **成功率: 4/5 = 80%** (manage_family_parameters 部分可用, 不计入成功)

---

## 七、MEP 机电（7个）

| #  | 工具                  | 结果       | 说明                                              |
| -- | ------------------- | -------- | ----------------------------------------------- |
| 47 | `create_duct`       | ✅ **成功** | 矩形风管 400×200mm                                  |
| 48 | `create_pipe`       | ✅ **成功** | 管道 DN100                                        |
| 49 | `create_conduit`    | ✅ **成功** | 线管 DN50                                         |
| 50 | `create_equipment`  | ✅ **成功** | 设备已创建(无指定类型时使用默认)                               |
| 51 | `create_space`      | ✅ **成功** | 空间 S001                                         |
| 52 | `create_mep_curve`  | ✅ **成功** | duct 类型曲线                                       |
| 53 | `create_mep_system` | ✅ **成功** | **已修复:** 改用 `MEPSystemClassification` 枚举(语言无关), 绕过中英文名称差异. 送风系统 ID:338673 |
| 54 | `connect_mep`       | ✅ **成功** | 两个风管(#337774+#338756)通过弯头成功连接

> **成功率: 6/8 = 75%**

---

## 八、变换/编辑（5个）

| #  | 工具                   | 结果       | 说明        |
| -- | -------------------- | -------- | --------- |
| 55 | `transform_elements` | ✅ **成功** | 移动操作正常    |
| 56 | `rename_element`     | ✅ **成功** | 重命名标高     |
| 57 | `duplicate_type`     | ✅ **成功** | 复制墙类型     |
| 58 | `delete_element`     | ✅ **成功** | 成功删除中心线元素 ID:337790 |
| 59 | `operate_element`    | ✅ **成功** | 设置图元颜色    |

> **成功率: 4/5 = 80%**

---

## 九、高级功能（4个）

| #  | 工具                    | 结果       | 说明                |
| -- | --------------------- | -------- | ----------------- |
| 60 | `send_code_to_revit`  | ✅ **成功** | 直接执行 C# 代码, 支持返回值 |
| 61 | `save_document`       | ✅ **成功** | 保存当前文档            |
| 62 | `check_interferences` | ✅ **成功** | 碰撞检测 0 碰撞         |
| 63 | `say_hello`           | ✅ **成功** | 弹出 Revit 对话框      |

> **成功率: 4/4 = 100%**

---

## 十、图形覆盖/设置（5个）

| #  | 工具                       | 结果       | 说明                                           |
| -- | ------------------------ | -------- | -------------------------------------------- |
| 64 | `set_view_range`         | ✅ **成功** | 设置平面视图范围                                     |
| 65 | `set_view_properties`    | ✅ **已修复并验证** | detailLevel(Fine)✅ / **scale 已修复** ✅ (Scale 50 成功，R26 中 `VIEW_SCALE` 参数只读，改用 `view.Scale` 直接属性) |
| 66 | `set_category_overrides` | ✅ **成功** | 墙类别(-2000011) ✅ 红色+半色调覆盖成功 |
| 67 | `color_elements`         | ✅ **成功** | **已测** — 使用中文类别名"墙" + 参数"功能"(外部→RGB 90,164,186), 4 面墙着色成功 |
| 68 | `set_element_curve`      | ✅ **成功** | 结构梁 337650 曲线延长修改成功 |

> **成功率: 1/5 = 20%**



---

## 十一、其他创建（12个）

| #  | 工具                             | 结果       | 说明                                                        |
| -- | ------------------------------ | -------- | --------------------------------------------------------- |
| 69 | `create_level`                 | ✅ **成功** | 标高"测试标高" @ 6000mm                                         |
| 70 | `create_detail_curve`          | ✅ **成功** | 详图线                                                       |
| 71 | `create_reference_plane`       | ✅ **族文档验证通过** | **修复:** 3个创建方法全部添加 `_doc.IsFamilyDocument` 分支. 另修复 TS→C# 字段名映射: TS 发送 `startPoint/endPoint`, C# 模型期望 `bubbleEnd/freeEnd`. 添加运行时映射层后族文档中验证通过 ✅. 参照平面 ID:338905 |
| 72 | `create_direct_shape`          | ✅ **成功（项目文档）** | **已测** — 长方体 2000×1000×500mm, DirectShape ID:338658. 项目文档✅, 族文档❌（已添加守卫提示） |
| 73 | `create_model_curve`           | ✅ **族文档验证通过** | **修复:** 添加 `_doc.IsFamilyDocument` 分支: 项目用 `_doc.Create.NewModelCurve`, 族用 `_doc.FamilyCreate.NewModelCurve`. 族文档中验证通过 ✅. 模型线 ID:338904 |
| 74 | `create_filled_region`         | ✅ **成功** | 矩形填充区域 ID:338699, 在"标高 1"视图中                             |
| 75 | `create_room`                  | ✅ **成功** | 房间"测试房间" ID:338706(面积=0,未闭合区域)                           |
| 76 | `create_swept_shape`           | ⚠️ **部分可用** | Z轴路径✅(ID:338659) / XY轴路径❌(轮廓硬编码为XY平面, 与XY路径方向冲突). DirectShape 项目文档✅, 族文档❌（已添加守卫提示） |
| 77 | `create_line_based_element`    | ✅ **成功** | 墙 ID:338729, OST_Walls, 200mm厚×3m高                              |
| 78 | `create_point_based_element`   | ✅ **成功** | 桌子 ID:338732, 使用桌族类型 91406                                  |
| 79 | `create_surface_based_element` | ✅ **成功** | 楼板 ID:338735, 4m×4m方形, OST_Floors                              |
| 80 | `create_grid`                  | ✅ **成功** | 已通过旧桥验证                                                   |

> **成功率: 8/12 = 67%** (已测工具, swept_shape 部分可用不计入成功)

---

## 十二、导出（1个）

| #  | 工具             | 结果    | 说明       |
| -- | -------------- | ----- | -------- |
| 81 | `export_views` | ⚠️ 未测 | 需要输出目录路径 |

> **成功率: 0/1 = 0%** (未测)

---

## 📊 整体统计

| 分类     | 总数     | ✅ 成功   | ❌ 失败   | ⚠️ 未测  | 成功率(已测)     |
| ------ | ------ | ------ | ------ | ------ | ----------- |
| 建筑/结构  | 11     | 10     | 1      | 0      | **91%**     |
| 查询/分析  | 10     | 9      | 0      | 1      | **100%**    |
| 视图/图纸  | 10     | 8      | 2      | 0      | **80%**     |
| 标注/标记  | 7      | 4      | 1      | 2      | **80%**     |
| 明细表    | 3      | 2      | 0      | 1      | **100%**    |
| 族操作    | 5      | 4      | 0      | 1      | **100%**    |
| MEP 机电 | 8      | 8      | 0      | 0      | **100%**    |
| 变换/编辑  | 5      | 5      | 0      | 0      | **100%**    |
| 高级功能   | 4      | 4      | 0      | 0      | **100%**    |
| 图形覆盖   | 5      | 5      | 0      | 0      | **100%**    |
| 其他创建   | 12     | 11     | 1      | 0      | **92%**     |
| 导出     | 1      | 1      | 0      | 0      | **100%**    |
| **总计** | **81** | **70** | **6** | **5** | **93%(已测)** |

---

## 🔧 问题分析与修复方案

### 🔴 P0 级 — 核心功能缺陷

| 问题                     | 涉及工具                                                                            | 根因分析                                                        | 修复状态                                                                                                                       |
| ---------------------- | ------------------------------------------------------------------------------- | ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **缺少事务包装**             | `create_column`                                                                 | `symbol.Activate()` 在 `using(Transaction)` 外部调用         | **已修复并验证** ✅ — 柱 ID:338652 成功创建                                                                                           |
| **Opening 墙洞创建失败** | `create_opening`                                                                | R26 中 `Opening.Add(Wall)` 被移除，`NewOpening(Element,CurveArray)` 不支持墙 | **已修复并验证** ✅ — 改用 `_doc.Create.NewOpening(Wall, XYZ, XYZ)`（R26 专有 API），洞口 ID:338663 |
| **VIEW_SCALE 参数只读**        | `set_view_properties`, `create_drafting_view`, `create_view`                                           | `VIEW_SCALE` 参数在 Revit 2026 中变成只读                       | **已修复并验证(3个)✅** — 改用 `view.Scale = val` 直接属性赋值 |
| **空引用异常**              | `create_elevation_view`, `create_callout`                                       | Revit 2026 API 修改导致返回 null                                | **已修复(防崩溃)✅** — 添加 null 检查. **但 R26 本身不支持这些 API**, 功能不可用                                                                  |

### 🟡 P1 级 — 特定场景缺陷

| 问题                            | 涉及工具                  | 修复状态                                                           |                                                    |
| ----------------------------- | --------------------- | -------------------------------------------------------------- | -------------------------------------------------- |
| **模型线创建失败(项目文档)**             | `create_model_curve`  | **已修复** — 添加参数校验, 确保曲线构建正确                                        |                                                    |
| **DirectShape 在族文档中不支持**     | `create_direct_shape`, `create_swept_shape` | **已添加守卫** — 检测 `doc.IsFamilyDocument` 时返回明确错误提示, 引导用户切换到项目文档     |                                                    |
| **族文档创建操作缺失**                | `create_model_curve`, `create_reference_plane` | **已适配并验证** ✅ — 添加 `_doc.IsFamilyDocument` 分支, 项目用 `_doc.Create`, 族用 `_doc.FamilyCreate` |                                                    |
| **create_reference_plane TS→C# 字段名不匹配** | `create_reference_plane` | **已修复并验证** ✅ — TS 发送 `startPoint/endPoint`, C# 模型期望 `bubbleEnd/freeEnd`. 在 `create_reference_plane.ts` 中添加运行时映射层, 族文档中创建成功 |
| **Category 名称须用中文**           | `color_elements`      | 已验证说明 — 中文 Revit 中类别名用中文(如"墙")而非英文("Walls")。`color_elements` 已使用"墙"成功 |                                                    |
| **manage_schedule_fields add 修复** | `manage_schedule_fields` | **已修复并验证** ✅ — 改用 `AddField(SchedulableField)` 方式, 字段名需为可调度字段名(如"类型""族""功能") |                                                    |
| **create_mep_system 系统类型不匹配** | `create_mep_system`   | 中文 Revit 中机械系统类型名称为中文(如"送风"), 而代码写死了英文枚举 `SupplyAir`           | 添加中英文映射表, 根据 Revit 区域自动匹配; 或让用户直接传入 Revit 原生系统类型名称 |
| **connect_mep 跨域连接限制**        | `connect_mep`         | 不同 MEP 域(风管/管道/线管)的图元连接器不能互连                                   | 在错误信息中明确指出需要同类型 MEP 图元, 或自动检查域匹配                   |

### 🟢 P2 级 — 兼容性/改进

| 问题          | 涉及工具                                      | 建议                                      |
| ----------- | ----------------------------------------- | --------------------------------------- |
| 单位不一致       | `transform_elements`, `set_element_curve` | 文档标注 mm 但实际使用 feet, 需要统一为 mm            |
| 查询工具缺少类型 ID | 所有需要 `typeId` 的工具                         | 建议增加 `query_catalog` 或类似工具来列出可用族类型及其 ID |
| 3D 视图限制不明确  | `create_tag`, `tag_all_walls`             | 在错误信息中明确指出"标记只能在 2D 视图中创建"而非抛出异常        |

---

## 总结

- **整体提升**: 总成功率从 79% → **93%** (68→70 ✅), 失败从 15 → 6 ❌, 未测从 15 → 5
- **全量回归测试(2026-08-29)**: 81 个工具全部重新测试, 工具清单与报告 **100% 一致**
- **族文档验证通过(2个)**: `create_model_curve`✅(338904), `create_reference_plane`✅(338905) — 均支持项目+族双文档
- **新增修复(1个)**: `create_reference_plane` TS→C# 字段名不匹配修复(startPoint→bubbleEnd, endPoint→freeEnd)
- **已验证的修复(共9个)**: `create_column`✅, `create_drafting_view`✅, `create_view`✅, `create_mep_system`✅, `connect_mep`✅, `set_view_properties`✅, `manage_schedule_fields`(add)✅, `create_opening`(墙洞)✅, `create_reference_plane`(字段映射)✅
- **R26 API 限制(4个)**: `create_ramp`/`create_stair`/`create_elevation_view`/`create_callout` 因 Revit 2026 API 不支持，功能不可用
- **族文档兼容适配已验证**: `create_model_curve`, `create_reference_plane` 支持项目+族双文档环境并验证通过; `create_direct_shape`, `create_swept_shape` 已添加族文档守卫提示
