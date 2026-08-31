# RevitMCPCommandSet 测试覆盖方案

## 现状

- **Service 文件总数**: ~200+
- **现有测试文件**: 7 个
- **测试覆盖率**: ~3%

## 测试框架

项目使用 `Nice3point.TUnit.Revit` 框架，基于 TUnit 的 Revit 集成测试。测试项目配置：

- 支持 R25 / R26 两个版本
- 测试文件位于 `tests/commandset/`
- 每个测试文件对应一个 Service 的 EventHandler

## 测试编写模式

参考现有测试（如 `CreateLevelTests.cs`），每个测试类遵循以下模式：

```csharp
[TestFixture]
public class XxxEventHandlerTests
{
    private UIApplication _uiApp;

    [SetUp]
    public void Setup()
    {
        _uiApp = Host.GetService<UIApplication>();
    }

    [Test]
    public void CreateXxx_WithValidParams_ReturnsSuccess()
    {
        // 1. 实例化 EventHandler
        var handler = new XxxEventHandler();

        // 2. 设置参数
        handler.SetParameters(...);

        // 3. 通过 ExternalEvent 执行
        var externalEvent = ExternalEvent.Create(handler);
        externalEvent.Raise();
        // 等待完成...

        // 4. 验证结果
        Assert.That(handler.Result.Success, Is.True);
        Assert.That(handler.Result.Response, Is.Not.Null);
    }
}
```

## 分阶段覆盖计划

### 第一阶段：Architecture 模块（12 个文件，优先级最高）

这些是 Revit 最核心的建筑元素创建功能。

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 1 | `CreateWallEventHandler.cs` | `CreateWallTests.cs` | 高 | 多种墙类型、曲线墙、参数化 |
| 2 | `CreateFloorEventHandler.cs` | `CreateFloorTests.cs` | 高 | 形状编辑、坡度、结构层 |
| 3 | `CreateCeilingEventHandler.cs` | `CreateCeilingTests.cs` | 中 | 自动边界、坡度 |
| 4 | `CreateRoofEventHandler.cs` | `CreateRoofTests.cs` | 高 | 迹线屋顶、拉伸屋顶 |
| 5 | `CreateStairEventHandler.cs` | `CreateStairTests.cs` | 高 | 多跑楼梯、平台、栏杆 |
| 6 | `CreateRampEventHandler.cs` | `CreateRampTests.cs` | 中 | 坡道创建、类型 |
| 7 | `CreateRailingEventHandler.cs` | `CreateRailingTests.cs` | 中 | 栏杆路径、高度 |
| 8 | `CreateColumnEventHandler.cs` | `CreateColumnTests.cs` | 中 | 结构柱、建筑柱 |
| 9 | `CreateOpeningEventHandler.cs` | `CreateOpeningTests.cs` | 中 | 墙/楼板/屋顶开洞 |
| 10 | `CreateModelCurveEventHandler.cs` | `CreateModelCurveTests.cs` | 中 | 线/弧/样条曲线 |
| 11 | `CreateReferencePlaneEventHandler.cs` | `CreateReferencePlaneTests.cs` | 低 | 参考平面创建 |
| 12 | `CreateGroupEventHandler.cs` | `CreateGroupTests.cs` | 中 | 成组、放置组实例 |

### 第二阶段：Views 模块（19 个文件，优先级高）

视图管理是 Revit 的核心交互方式。

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 13 | `CreateViewEventHandler.cs` | `CreateViewTests.cs` | 高 | 楼层/天花板/立面/剖面/3D 视图 |
| 14 | `CreateElevationViewEventHandler.cs` | `CreateElevationViewTests.cs` | 高 | 立面视图、方向 |
| 15 | `CreateScheduleEventHandler.cs` | `CreateScheduleTests.cs` | 高 | 明细表创建、字段 |
| 16 | `CreateSheetEventHandler.cs` | `CreateSheetTests.cs` | 中 | 图纸创建、修订 |
| 17 | `CreateCalloutEventHandler.cs` | `CreateCalloutTests.cs` | 中 | 详图索引 |
| 18 | `CreateDraftingViewEventHandler.cs` | `CreateDraftingViewTests.cs` | 中 | 绘图视图 |
| 19 | `CreateFilledRegionEventHandler.cs` | `CreateFilledRegionTests.cs` | 低 | 填充区域 |
| 20 | `CreateViewTemplateEventHandler.cs` | `CreateViewTemplateTests.cs` | 低 | 视图样板 |
| 21 | `DuplicateViewEventHandler.cs` | `DuplicateViewTests.cs` | 低 | 视图复制 |
| 22 | `SetViewRangeEventHandler.cs` | `SetViewRangeTests.cs` | 中 | 视图范围设置 |
| 23 | `SetViewPropertiesEventHandler.cs` | `SetViewPropertiesTests.cs` | 中 | 视图属性 |
| 24 | `SetCategoryOverridesEventHandler.cs` | `SetCategoryOverridesTests.cs` | 中 | 类别可见性/图形替换 |
| 25 | `PlaceViewOnSheetEventHandler.cs` | `PlaceViewOnSheetTests.cs` | 中 | 视图布图 |
| 26 | `PlaceScheduleOnSheetEventHandler.cs` | `PlaceScheduleOnSheetTests.cs` | 低 | 明细表布图 |
| 27 | `ManageScheduleFieldsEventHandler.cs` | `ManageScheduleFieldsTests.cs` | 中 | 明细表字段管理 |
| 28 | `ManageViewFiltersEventHandler.cs` | `ManageViewFiltersTests.cs` | 中 | 视图过滤器 |
| 29 | `ExportViewsEventHandler.cs` | `ExportViewsTests.cs` | 中 | 视图导出（DWG/IFC/图片） |
| 30 | `CreateDetailCurveEventHandler.cs` | `CreateDetailCurveTests.cs` | 低 | 详图线 |
| 31 | `QueryViewRangeEventHandler.cs` | `QueryViewRangeTests.cs` | 中 | 查询视图范围 |

### 第三阶段：MEP 模块（11 个文件，优先级中）

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 32 | `CreateDuctEventHandler.cs` | `CreateDuctTests.cs` | 高 | 风管创建、尺寸、系统 |
| 33 | `CreatePipeEventHandler.cs` | `CreatePipeTests.cs` | 高 | 管道创建、尺寸、系统 |
| 34 | `CreateConduitEventHandler.cs` | `CreateConduitTests.cs` | 中 | 线管创建 |
| 35 | `CreateMEPCurveEventHandler.cs` | `CreateMEPCurveTests.cs` | 高 | 通用 MEP 曲线 |
| 36 | `CreateMEPSystemEventHandler.cs` | `CreateMEPSystemTests.cs` | 高 | MEP 系统创建 |
| 37 | `ConnectMEPEventHandler.cs` | `ConnectMEPTests.cs` | 中 | MEP 连接 |
| 38 | `CreateSpaceEventHandler.cs` | `CreateSpaceTests.cs` | 中 | 空间创建 |
| 39 | `PlaceFamilyInstanceEventHandler.cs` | `PlaceFamilyInstanceTests.cs` | 中 | 族实例放置 |
| 40 | `CreateEquipmentEventHandler.cs` | `CreateEquipmentTests.cs` | 中 | 设备放置 |
| 41 | `CreateDirectShapeEventHandler.cs` | `CreateDirectShapeTests.cs` | 中 | 直接形状 |
| 42 | `CreateSweptShapeEventHandler.cs` | `CreateSweptShapeTests.cs` | 中 | 扫略形状 |

### 第四阶段：Modify 模块（8 个文件，优先级中）

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 43 | `ManageFamilyParametersEventHandler.cs` | `ManageFamilyParametersTests.cs` | 高 | 族参数管理 |
| 44 | `ManageProjectParametersEventHandler.cs` | `ManageProjectParametersTests.cs` | 高 | 项目参数管理 |
| 45 | `ManageGraphicsResourcesEventHandler.cs` | `ManageGraphicsResourcesTests.cs` | 中 | 图形资源（线型/颜色） |
| 46 | `TransformElementsEventHandler.cs` | `TransformElementsTests.cs` | 中 | 移动/旋转/镜像 |
| 47 | `SetParametersEventHandler.cs` | `SetParametersTests.cs` | 中 | 设置元素参数 |
| 48 | `SetElementCurveEventHandler.cs` | `SetElementCurveTests.cs` | 低 | 修改元素曲线 |
| 49 | `RenameElementEventHandler.cs` | `RenameElementTests.cs` | 低 | 重命名元素 |
| 50 | `DuplicateTypeEventHandler.cs` | `DuplicateTypeTests.cs` | 低 | 复制类型 |

### 第五阶段：Query 模块（6 个文件，优先级中）

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 51 | `QueryGeometryEventHandler.cs` | `QueryGeometryTests.cs` | 中 | 几何查询 |
| 52 | `CheckInterferencesEventHandler.cs` | `CheckInterferencesTests.cs` | 中 | 碰撞检查 |
| 53 | `QueryReferencesEventHandler.cs` | `QueryReferencesTests.cs` | 低 | 引用查询 |
| 54 | `QueryParametersEventHandler.cs` | `QueryParametersTests.cs` | 低 | 参数查询 |
| 55 | `QueryViewRangeEventHandler.cs` | `QueryViewRangeTests.cs` | 中 | 视图范围查询 |
| 56 | `QueryCurtainGridEventHandler.cs` | `QueryCurtainGridTests.cs` | 中 | 幕墙网格查询 |

### 第六阶段：Annotation 模块（4 个文件，优先级低）

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 57 | `CreateTextNoteEventHandler.cs` | `CreateTextNoteTests.cs` | 低 | 文字注释 |
| 58 | `CreateTagEventHandler.cs` | `CreateTagTests.cs` | 中 | 标记 |
| 59 | `CreateRevisionCloudEventHandler.cs` | `CreateRevisionCloudTests.cs` | 中 | 修订云线 |
| 60 | `CreateDimensionEventHandler.cs` | `CreateDimensionTests.cs` | 中 | 尺寸标注 |

### 第七阶段：其他顶层文件（17 个文件，优先级低）

| # | Service 文件 | 测试文件 | 复杂度 | 说明 |
|---|-------------|---------|--------|------|
| 61 | `AIElementFilterEventHandler.cs` | `AIElementFilterTests.cs` | 极高 | AI 元素过滤（最复杂） |
| 62 | `LoadFamilyEventHandler.cs` | `LoadFamilyTests.cs` | 中 | 载入族 |
| 63 | `DeleteElementEventHandler.cs` | `DeleteElementTests.cs` | 低 | 删除元素 |
| 64 | `CreateGridEventHandler.cs` | `CreateGridTests.cs` | 中 | 创建轴线 |
| 65 | `CreateLineElementEventHandler.cs` | `CreateLineElementTests.cs` | 低 | 创建线元素 |
| 66 | `CreatePointElementEventHandler.cs` | `CreatePointElementTests.cs` | 低 | 创建点元素 |
| 67 | `CreateSurfaceElementEventHandler.cs` | `CreateSurfaceElementTests.cs` | 中 | 创建面元素 |
| 68 | `CreateStructuralFramingSystemEventHandler.cs` | `CreateStructuralFramingTests.cs` | 中 | 结构框架系统 |
| 69 | `OperateElementEventHandler.cs` | `OperateElementTests.cs` | 中 | 元素操作 |
| 70 | `GetCurrentViewInfoEventHandler.cs` | `GetCurrentViewInfoTests.cs` | 低 | 获取当前视图信息 |
| 71 | `GetCurrentViewElementsEventHandler.cs` | `GetCurrentViewElementsTests.cs` | 低 | 获取当前视图元素 |
| 72 | `GetSelectedElementsEventHandler.cs` | `GetSelectedElementsTests.cs` | 低 | 获取选中元素 |
| 73 | `GetAvailableFamilyTypesEventHandler.cs` | `GetAvailableFamilyTypesTests.cs` | 低 | 获取可用族类型 |
| 74 | `SaveDocumentEventHandler.cs` | `SaveDocumentTests.cs` | 低 | 保存文档 |
| 75 | `SayHelloEventHandler.cs` | `SayHelloTests.cs` | 极低 | 简单测试 |
| 76 | `TagWallsEventHandler.cs` | `TagWallsTests.cs` | 中 | 标记墙（已有部分） |
| 77 | `ColorSplashEventHandler.cs` | `ColorSplashTests.cs` | 中 | 颜色填充（已有部分） |

## 实施优先级建议

```
第一优先级（P0）：Architecture 模块（12 个）
  └── 核心建筑功能，用户最常用

第二优先级（P1）：Views 模块（19 个）
  └── 视图管理，与 Architecture 配合使用

第三优先级（P2）：MEP 模块（11 个）
  └── 机电功能，复杂度高

第四优先级（P3）：Modify + Query 模块（14 个）
  └── 修改和查询功能

第五优先级（P4）：Annotation + 顶层文件（21 个）
  └── 标注和其他功能
```

## 工作量估算

| 阶段 | 文件数 | 估算人天 | 说明 |
|------|--------|---------|------|
| P0: Architecture | 12 | 6-8 | 每个文件 0.5-0.7 天 |
| P1: Views | 19 | 8-12 | 每个文件 0.4-0.6 天 |
| P2: MEP | 11 | 5-8 | 每个文件 0.5-0.7 天 |
| P3: Modify+Query | 14 | 5-7 | 每个文件 0.3-0.5 天 |
| P4: Annotation+其他 | 21 | 7-10 | 每个文件 0.3-0.5 天 |
| **总计** | **77** | **31-45** | |

## 注意事项

1. **Revit 环境依赖**：测试需要 Revit 运行环境，不能纯单元测试
2. **版本兼容**：测试需覆盖 R25/R26 两个配置
3. **事务管理**：测试中需正确处理 Transaction，测试完成后回滚
4. **模型准备**：部分测试需要预先创建特定的 Revit 元素（如族类型、标高）
5. **异步等待**：使用 `ExternalEvent` 执行后需要等待完成
6. **参数验证**：需覆盖正常参数、边界参数、无效参数三种场景
