# Revit 闪退风险详细修复方案

> 分析日期: 2026-08-28
> 参考项目: `E:\WORKSPACE\CODE_REVIT\revit-mcp-local-bridge`
> 项目基线: `E:\WORKSPACE\CODE_REVIT\mcp-servers-for-revit`

---

## 目录

- [P0 — 导致闪退的严重缺陷](#p0--导致闪退的严重缺陷)
  - [P0-1: GetName() 方法作用域错误](#p0-1-getname-方法作用域错误)
  - [P0-2: 后台线程非法访问 Revit API](#p0-2-后台线程非法访问-revit-api)
  - [P0-3: ActiveUIDocument 无空值检查](#p0-3-activeuidocument-无空值检查)
- [P1 — 高崩溃概率风险](#p1--高崩溃概率风险)
  - [P1-1: FilteredElementCollector 未释放](#p1-1-filteredelementcollector-未释放)
  - [P1-2: LoadFamily 在 Transaction 内调用](#p1-2-loadfamily-在-transaction-内调用)
  - [P1-3: doc.Regenerate() 在 Transaction 内调用](#p1-3-docregenerate-在-transaction-内调用)
  - [P1-4: 空 catch {} 吞掉所有异常](#p1-4-空-catch--吞掉所有异常)
  - [P1-5: TaskDialog.Show() 阻塞 ExternalEvent](#p1-5-taskdialogshow-阻塞-externalevent)
  - [P1-6: 事务失败时 Commit 替代 RollBack](#p1-6-事务失败时-commit-替代-rollback)
  - [P1-7: Assembly.LoadFrom 锁定 DLL](#p1-7-assemblyloadfrom-锁定-dll)
- [P2 — 中风险（长期稳定性）](#p2--中风险长期稳定性)
  - [P2-1: ManualResetEvent 泄漏](#p2-1-manualresetevent-泄漏)
  - [P2-2: 空事务 Commit 替代 RollBack (MEP)](#p2-2-空事务-commit-替代-rollback-mep)
  - [P2-3: 静态锁导致并发串行化](#p2-3-静态锁导致并发串行化)
  - [P2-4: InternalError 异常信息丢失](#p2-4-internalerror-异常信息丢失)
  - [P2-5: EventHandler 生命周期管理](#p2-5-eventhandler-生命周期管理)
- [系统性架构改进](#系统性架构改进)
- [操作清单（按文件）](#操作清单按文件)

---

## P0 — 导致闪退的严重缺陷

### P0-1: GetName() 方法作用域错误

**影响:** **必定闪退**，Revit 调用 `IExternalEventHandler.GetName()` 抛出 `MissingMethodException`

**文件:** `commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs`

**原因分析:**

第 96-162 行结构如下：

```
96:    private object CompileAndExecuteCode(...)
97:    {
98:        ...
156:    }   ← CompileAndExecuteCode 方法体闭合
157:
158:    public string GetName()    ← 仍在 CompileAndExecuteCode 内部！
159:    {
160:        return "执行AI代码";
161:    }
162: }    ← 这里闭合的是 CompileAndExecuteCode 而非类 body
```

第 157 行的 `}` 实际上是 `CompileAndExecuteCode` 的闭合，但第 158-161 行的 `GetName()` 被错误地放置在了该方法内部。在执行编译后的代码时，C# 不允许在非局部函数的方法内定义另一个方法，但这里的代码之所以能编译，是因为实际上 `GetName()` 被 C# 编译器当作了类级别的方法（注意第 162 行的 `}` 闭合的是 `CompileAndExecuteCode`，而第 156 行才是实际的方法闭合）。**实际结构分析：**

仔细看第 96-162 行的实际大括号匹配：

- L96: `private object CompileAndExecuteCode(string code, Document doc, object[] parameters)` ← 方法声明
- L97: `{` ← 方法体开始
- L156: `}` ← 方法体结束
- L157: 空行
- L158-161: GetName() 方法声明——这是类级别的方法声明，不是嵌套的
- L162: `}` ← 这里显然是类的结束？不对，如果 L156 闭合了方法，L158-161 是类级方法，L162 闭合的是什么？

看完整文件的结尾：
- L15: `public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler`
- L15: `{` ← 类开始
- L96-156: CompileAndExecuteCode 方法
- L157: 空行
- L158-161: `GetName()`
- L162: `}` ← 类结束
- L164-175: `ExecutionResultInfo` 类（与 `ExecuteCodeEventHandler` 同级）

所以 `GetName()` 实际上是 `ExecuteCodeEventHandler` 的类级方法。它没有嵌套问题。但 `ExecutionResultInfo` 被定义在了类外部（第 164-175 行），但它是一个独立的类定义，不在 `ExecuteCodeEventHandler` 内部，所以这是合法的。

修正：这个 `GetName()` 方法实际上**没有作用域错误**。问题在于它被**遗漏**了——它本应作为 `IExternalEventHandler` 的接口方法实现。它确实被实现了，但位置在 `CompileAndExecuteCode` 方法之后、类结束之前，写法是正确的。

**⚠️ 重新评估：此问题实际不存在。撤回该发现。**

---

### P0-2: 后台线程非法访问 Revit API

**影响:** **高概率闪退**，Revit API 对象在非 UI 线程上访问

**文件:** 
- `plugin/Core/SocketService.cs` (L108, L150 — 线程创建)
- `plugin/Core/SocketService.cs` (L253 — background thread calls command.Execute())
- `plugin/Core/CommandExecutor.cs` (L45 — same path called from SocketService)

**调用链分析:**

```
SocketService.ListenForClients()          ← 线程 A (new Thread)
  └─ HandleClientCommunication()          ← 线程 B (new Thread per client)
       └─ ProcessJsonRPCRequest()         ← 线程 B
            └─ command.Execute()          ← 线程 B: IRevitCommand.Execute() on background thread
                 └─ RaiseAndWaitForCompletion() → ExternalEvent.Raise() → UI 线程
                     └─ EventHandler.Execute(UIApplication) → UI 线程，安全
```

**现状:** 虽然 `command.Execute()` 内部调用 `RaiseAndWaitForCompletion()` 将实际 Revit API 操作调度到 UI 线程，但 **`command.Execute()` 方法本身**在调用 `RaiseAndWaitForCompletion()` **之前**已经运行在后台线程上。查看 `DeleteElementCommand.Execute()`:

```csharp
// DeleteElementCommand.cs:20-27
public override object Execute(JObject parameters, string requestId)
{
    lock (_executionLock)                    ← 后台线程
    {
        // 解析参数（不访问 Revit API，安全）
        var elementIds = parameters?["elementIds"]?.ToObject<string[]>();

        _handler.ElementIds = elementIds;    ← 设置属性，安全
        if (RaiseAndWaitForCompletion(15000)) ← 调度到 UI 线程
```

参数解析本身是 JSON 操作，不涉及 Revit API 对象，**当前代码在这种情况下是安全的**。但如果任何命令的 `Execute()` 方法在 `RaiseAndWaitForCompletion()` **之前**访问了 `Document`、`Element` 等 Revit API 对象，就会崩溃。

**修复方案 A（推荐）——统一调度到 UI 线程执行整条命令：**

在 `SocketService.ProcessJsonRPCRequest` 中，将命令查找 + 执行整体放到 UI 线程：

```csharp
// SocketService.cs - 新增方法
private string ExecuteOnUIThread(Func<string> action)
{
    string result = null;
    Exception exception = null;
    
    // 使用 ExternalEvent 调度到 UI 线程
    var handler = new AnonymousEventHandler(() =>
    {
        try { result = action(); }
        catch (Exception ex) { exception = ex; }
    });
    
    var evt = ExternalEvent.Create(handler);
    evt.Raise();
    // 等待完成...
    
    if (exception != null) throw exception;
    return result;
}

// 然后 ProcessJsonRPCRequest 中改为：
private string ProcessJsonRPCRequest(string requestJson) 
{
    // ... 先解析 JSON（不涉及 Revit API，可在后台线程做）
    // 然后执行命令调度到 UI 线程
    return ExecuteOnUIThread(() => {
        object result = command.Execute(request.GetParamsObject(), request.Id);
        return CreateSuccessResponse(request.Id, result);
    });
}
```

**`AnonymousEventHandler` 实现:**

```csharp
// 新增文件或放在已有文件中
internal class AnonymousEventHandler : IExternalEventHandler
{
    private readonly Action _action;
    private readonly ManualResetEvent _done = new ManualResetEvent(false);
    
    public AnonymousEventHandler(Action action) { _action = action; }
    
    public void Execute(UIApplication app)
    {
        try { _action(); }
        finally { _done.Set(); }
    }
    
    public void WaitForCompletion(int timeout = 30000) => _done.WaitOne(timeout);
    public string GetName() => "AnonymousDispatch";
}
```

**修复方案 B（轻量保险）——为所有命令的 Execute 加保护断言：**

```csharp
// 在 ExternalEventCommandBase 或每个命令开头：
public override object Execute(JObject parameters, string requestId)
{
    // 确保在 UI 线程上
    if (!Autodesk.Revit.ApplicationServices.Application.IsInRevitMainThread())
    {
        throw new InvalidOperationException(
            "Command must be executed on Revit UI thread");
    }
    // ...
}
```

---

### P0-3: ActiveUIDocument 无空值检查

**影响:** 无文档打开时调用 → `NullReferenceException` → **闪退**

**文件:** `plugin/Core/ExternalEventManager.cs:66`

**当前代码 (L64-70):**

```csharp
ExternalEvent externalEvent = null;

// 使用活动文档的上下文执行创建事件的操作
_uiApp.ActiveUIDocument.Document.Application.ExecuteCommand(
    (uiApp) => {
        externalEvent = ExternalEvent.Create(handler);
    }
);
```

**问题链:** `_uiApp.ActiveUIDocument` → 若为 null → `.Document` → NRE

**更关键的问题:** `ExternalEvent.Create()` 根本**不依赖**于 `ActiveUIDocument`。它只需要在 UI 线程上调用即可。当前代码通过 `Document.Application.ExecuteCommand` 绕了一个大圈来确保 UI 线程上下文，但这一操作本身就需要 `ActiveUIDocument`。

**修复:**

```csharp
// 方案A（推荐）——使用 UIControlledApplication 直接创建
// 在 Initialize 时保存 UIControlledApplication 或使用 Idling 方式

// 方案B——使用 IExternalApplication 的 OnStartup 预先创建
// 修改 Application.cs，在 OnStartup 中预初始化 ExternalEventManager

// 方案C——最安全的临时修复
ExternalEvent externalEvent = null;

// 判断是否有活动文档
if (_uiApp.ActiveUIDocument == null)
{
    // 无文档时：通过 Idling 事件创建
    // 或者延迟到有文档时再创建
    _logger.Warning("No active document - deferring ExternalEvent creation");
    return null; // 或抛出明确异常
}

_uiApp.ActiveUIDocument.Document.Application.ExecuteCommand(
    (uiApp) => {
        externalEvent = ExternalEvent.Create(handler);
    }
);
```

**更好的架构方案——修改 ExternalEventManager：**

```csharp
public class ExternalEventManager
{
    // 存储对 UIControlledApplication 的引用（在 Initialize 时获得）
    private UIControlledApplication _uiControlledApp;
    
    public void Initialize(UIControlledApplication app, ILogger logger)
    {
        _uiControlledApp = app;
        _logger = logger;
        _isInitialized = true;
    }
    
    // 或者在 Application.OnStartup 中直接预先创建所有 ExternalEvent
}
```

但 `IExternalApplication` 提供的是 `UIControlledApplication`，它不能在 `OnStartup` 中创建 `ExternalEvent`。正确的做法是在 `MCPServiceConnection`（一个 `IExternalCommand`）或在第一次需要时使用一个临时的 UI 线程调度。

**最简修复（ExternalEventManager.cs L66-70）：**

```csharp
ExternalEvent externalEvent = null;

if (_uiApp.ActiveUIDocument == null)
{
    throw new InvalidOperationException(
        "Cannot create ExternalEvent: no active document. " +
        "Please open a Revit document first.");
}

_uiApp.ActiveUIDocument.Document.Application.ExecuteCommand(
    (uiApp) => {
        externalEvent = ExternalEvent.Create(handler);
    }
);
```

---

## P1 — 高崩溃概率风险

### P1-1: FilteredElementCollector 未释放

**影响:** 非托管资源泄漏 → 长会话内存压力 → Revit 无响应/崩溃

**涉及文件（共 15 个文件，20+ 处）：**

| 文件 | 行号 | 模式 |
|------|------|------|
| `ColorSplashEventHandler.cs` | 80, 439 | `new FilteredElementCollector(doc, ...)` → `ToElements()` |
| `CreateGridEventHandler.cs` | 57 | `new FilteredElementCollector(doc)` → `ToHashSet()` |
| `CreateLineElementEventHandler.cs` | 102 | `new FilteredElementCollector(doc)` |
| `CreatePointElementEventHandler.cs` | 82, 89 | `new FilteredElementCollector(doc).OfClass(...)` |
| `CreateStructuralFramingSystemEventHandler.cs` | 66 | `new FilteredElementCollector(doc).OfClass(...)` |
| `CreateSurfaceElementEventHandler.cs` | 105 | `new FilteredElementCollector(doc)` |
| `GetAvailableFamilyTypesEventHandler.cs` | 34 | `new FilteredElementCollector(doc)` |
| `GetCurrentViewElementsEventHandler.cs` | 94 | `new FilteredElementCollector(doc, viewId)` |
| `LoadFamilyEventHandler.cs` | 57 | `new FilteredElementCollector(doc)` |
| `OperateElementEventHandler.cs` | 135 | `new FilteredElementCollector(doc)` |
| `TagRoomsEventHandler.cs` | 90, 119, 170, 183, 386 | 多处 `new FilteredElementCollector(_doc, ...)` |
| `TagWallsEventHandler.cs` | 44 | `new FilteredElementCollector(doc, viewId)` |
| `AIElementFilterEventHandler.cs` | 165, 170 | `new FilteredElementCollector(doc, ...)` |

**通用修复模板（每处替换）：**

```csharp
// ❌ 修改前
FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id);
var elements = collector.OfCategory(...).ToElements();

// ✅ 修改后
using (FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id))
{
    var elements = collector.OfCategory(...).ToElements();
}
```

**对于链式调用模式（如 TagRoomsEventHandler.cs:90）：**

```csharp
// ❌ 修改前
var anyRoom = new FilteredElementCollector(_doc)
    .OfCategory(BuiltInCategory.OST_Rooms)
    .WhereElementIsNotElementType()
    .Cast<Room>()
    .FirstOrDefault(r => r.Area > 0);

// ✅ 修改后
Room anyRoom;
using (var collector = new FilteredElementCollector(_doc))
{
    anyRoom = collector
        .OfCategory(BuiltInCategory.OST_Rooms)
        .WhereElementIsNotElementType()
        .Cast<Room>()
        .FirstOrDefault(r => r.Area > 0);
}
```

**特别情况：`ColorSplashEventHandler.cs:439`：**

```csharp
// ❌ 修改前
FilteredElementCollector collector = new FilteredElementCollector(doc);
collector.OfClass(typeof(FillPatternElement));
foreach (FillPatternElement patternElement in collector) { ... }

// ✅ 修改后
using (FilteredElementCollector collector = new FilteredElementCollector(doc))
{
    collector.OfClass(typeof(FillPatternElement));
    foreach (FillPatternElement patternElement in collector) { ... }
}
```

---

### P1-2: LoadFamily 在 Transaction 内调用

**影响:** 族加载触发的内部再生与事务冲突 → `InvalidOperationException` 或意外行为

**文件:** `commandset/Services/LoadFamilyEventHandler.cs:37-53`

**当前代码 (L37-53):**

```csharp
using (Transaction trans = new Transaction(doc, "Load Family"))
{
    trans.Start();

#if REVIT2026_OR_GREATER
    bool loaded = doc.LoadFamily(FilePath);
#elif REVIT2025_OR_GREATER
    FamilyLoadOptions loadOptions = new FamilyLoadOptions();
    bool loaded = doc.LoadFamily(FilePath, loadOptions);
#else
    bool loaded = doc.LoadFamily(FilePath);
#endif
    // ...
}
```

**修复：**

```csharp
// 第一步：在事务外 LoadFamily
#if REVIT2026_OR_GREATER
    bool loaded = doc.LoadFamily(FilePath);
#elif REVIT2025_OR_GREATER
    FamilyLoadOptions loadOptions = new FamilyLoadOptions();
    bool loaded = doc.LoadFamily(FilePath, loadOptions);
#else
    bool loaded = doc.LoadFamily(FilePath);
#endif

// 第二步：如果还需要事务做其他操作，再单独开事务
if (loaded && !string.IsNullOrEmpty(FamilyName))
{
    using (Transaction trans = new Transaction(doc, "Load Family"))
    {
        trans.Start();
        Family family = new FilteredElementCollector(doc)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(f => f.Name == FamilyName);
        // ... 任何需要在事务中做的操作
        trans.Commit();
    }
}
```

**注意：** 如果 `LoadFamily` 之后的业务逻辑纯粹是**查询**（查找 Family 名称等只读操作），则不需要任何事务。事务仅用于修改文档的操作。

---

### P1-3: doc.Regenerate() 在 Transaction 内调用

**影响:** 某些 Revit API 版本中触发 `AccessViolationException` 或文档事务栈损坏

**涉及文件：**

| 文件 | 行号 | 上下文 |
|------|------|--------|
| `CreatePointElementEventHandler.cs` | 152, 194 | `doc.Regenerate()` 在 Transaction 块内调用 |
| `TagRoomsEventHandler.cs` | 226 | `_doc.Regenerate()` 在 Transaction 块内 |
| `TagWallsEventHandler.cs` | 75 | `doc.Regenerate()` 在 Transaction 块内 |
| `CreateStructuralFramingSystemEventHandler.cs` | 138 | `doc.Regenerate()` 在 Transaction 块内 |

**修复原则：** `doc.Regenerate()` **永远**移到 `transaction.Commit()` 之后。

**CreatePointElementEventHandler.cs 修复 (L109-213):**

```csharp
using (Transaction transaction = new Transaction(doc, "创建点状构件"))
{
    transaction.Start();
    
    if (!symbol.IsActive)
        symbol.Activate();
    
    // ... 创建实例等操作
    
    // ❌ 移除这里的 Regenerate
    // doc.Regenerate();  ← L152
    
    // ... 开门方向检测等需要 Regenerate 的操作
    
    if (shouldFlip)
    {
        // ❌ 移除这里的 Regenerate
        // doc.Regenerate();  ← L194
        instance.flipFacing();
    }
    
    transaction.Commit();
}  // Transaction 在这里结束

// ✅ 将所有 Regenerate 移到事务外
doc.Regenerate();

// 或者，如果 Regenerate 是为了获取更新后的几何信息：
// 先 Commit，再 Regenerate，再做只读操作
```

**TagRoomsEventHandler.cs 修复 (L222-227):**

```csharp
// ✅ 移除事务内的 Regenerate
if (!roomTagType.IsActive)
{
    roomTagType.Activate();
    // ❌ 移除: _doc.Regenerate();
}

// 事务内不做 Regenerate
// 事务结束后做
```

---

### P1-4: 空 catch {} 吞掉所有异常

**影响:** 异常被完全静默，闪退排查无任何线索

**涉及文件：**

| 文件 | 行号 | 当前代码 |
|------|------|---------|
| `plugin/Core/Application.cs` | 42 | `catch { }` |
| `plugin/Core/SocketService.cs` | 114-117 | `catch (Exception) { _isRunning = false; }` |
| `plugin/Core/SocketService.cs` | 136-139 | `catch (Exception) { // log error }` |
| `plugin/Core/SocketService.cs` | 157-160 | `catch (SocketException) { }` (空) |
| `plugin/Core/SocketService.cs` | 161-164 | `catch(Exception) { // log }` (空) |
| `plugin/Core/SocketService.cs` | 211-214 | `catch(Exception) { // log }` (空) |

**Application.cs:OnShutdown 修复 (L33-45):**

```csharp
public Result OnShutdown(UIControlledApplication application)
{
    try
    {
        if (SocketService.Instance.IsRunning)
        {
            SocketService.Instance.Stop();
        }
    }
    catch (Exception ex)
    {
        // ❌ 原代码: catch { }
        // ✅ 写入日志
        System.Diagnostics.Debug.WriteLine($"[Application.OnShutdown] Error during shutdown: {ex}");
        // 可考虑使用 ILogger，但注意 OnShutdown 时 logger 可能已被释放
    }

    return Result.Succeeded;
}
```

**SocketService.cs 修复:**

```csharp
// L114-117 - Start() 中的异常
catch (Exception ex)
{
    _logger?.Error($"SocketService.Start failed: {ex.Message}");
    _isRunning = false;
}

// L136-139 - Stop() 中的异常
catch (Exception ex)
{
    _logger?.Error($"SocketService.Stop failed: {ex.Message}");
}

// L157-160 - ListenForClients SocketException
catch (SocketException ex)
{
    _logger?.Warning($"Socket closed ({ex.SocketErrorCode}): {ex.Message}");
}

// L161-164 - ListenForClients 其他异常
catch(Exception ex)
{
    _logger?.Error($"ListenForClients error: {ex.Message}");
}

// L211-214 - HandleClientCommunication 异常
catch(Exception ex)
{
    _logger?.Error($"HandleClientCommunication error: {ex.Message}");
}
```

---

### P1-5: TaskDialog.Show() 阻塞 ExternalEvent

**影响:** 模态对话框挂起 UI 线程，外部 MCP 客户端超时断开

**涉及文件（共 13 处）：**

| 文件 | 行号 | 调用 |
|------|------|------|
| `SayHelloEventHandler.cs` | 22 | `TaskDialog.Show("Revit MCP", Message)` |
| `DeleteElementEventHandler.cs` | 57, 76, 82 | `TaskDialog.Show("警告"/"错误", ...)` |
| `CreateGridEventHandler.cs` | 198 | `TaskDialog.Show("Error", ...)` |
| `CreateLineElementEventHandler.cs` | 253 | `TaskDialog.Show("错误", ...)` |
| `CreatePointElementEventHandler.cs` | 234 | `TaskDialog.Show("错误", ...)` |
| `CreateSurfaceElementEventHandler.cs` | 285 | `TaskDialog.Show("错误", ...)` |
| `GetAvailableFamilyTypesEventHandler.cs` | 118 | `TaskDialog.Show("Error", ...)` |
| `GetCurrentViewElementsEventHandler.cs` | 156 | `TaskDialog.Show("error", ...)` |
| `GetCurrentViewInfoEventHandler.cs` | 43 | `TaskDialog.Show("error", ...)` |
| `GetSelectedElementsEventHandler.cs` | 53 | `TaskDialog.Show("Error", ...)` |
| `TagRoomsEventHandler.cs` | 334 | `TaskDialog.Show("Error", ...)` |
| `TagWallsEventHandler.cs` | 184 | `TaskDialog.Show("错误", ...)` |
| `CreateStructuralFramingSystemEventHandler.cs` | 263 | `TaskDialog.Show("Error", ...)` |

**通用的修复策略：**

```csharp
// ❌ 修改前
catch (Exception ex)
{
    TaskDialog.Show("错误", $"创建点状构件时出错: {ex.Message}");
    Result = new AIResult<List<int>> { Success = false, Message = ex.Message };
}

// ✅ 修改后 - 异常信息通过 Result 返回，不阻塞 UI
catch (Exception ex)
{
    // 用 Debug 输出（开发调试用）
    System.Diagnostics.Debug.WriteLine($"[CreatePointElement] Error: {ex.Message}");
    
    // 通过 Result 将错误信息传回给调用方
    Result = new AIResult<List<int>>
    {
        Success = false,
        Message = $"创建点状构件时出错: {ex.Message}",
    };
}
```

**`SayHelloEventHandler.cs` 特殊处理 (L22):**

```csharp
// 这个 handler 本来就是测试用，Message 就是要显示给用户看的
// 方案：改为通过 ILogger 输出，或仅在 DEBUG 下弹窗
public void Execute(UIApplication app)
{
    try
    {
        // 通过日志输出而不是弹窗
        System.Diagnostics.Debug.WriteLine($"[SayHello] Message: {Message}");
#if DEBUG
        TaskDialog.Show("Revit MCP", Message);
#endif
    }
    finally
    {
        _resetEvent.Set();
    }
}
```

---

### P1-6: 事务失败时 Commit 替代 RollBack

**影响:** 实体找不到时提交空事务，污染文档修改状态，撤销栈被清空

**文件:** `commandset/Services/MEP/PlaceFamilyInstanceEventHandler.cs:44-46`

**当前代码 (L41-47):**

```csharp
Element symbolElem = doc.GetElement(new ElementId(data.SymbolId));
if (symbolElem == null || !(symbolElem is FamilySymbol symbol))
{
    _warnings.Add($"FamilySymbol with ID {data.SymbolId} not found.");
    transaction.Commit();  // ❌ 错误：空事务被 Commit
    continue;
}
```

**修复：**

```csharp
Element symbolElem = doc.GetElement(new ElementId(data.SymbolId));
if (symbolElem == null || !(symbolElem is FamilySymbol symbol))
{
    _warnings.Add($"FamilySymbol with ID {data.SymbolId} not found.");
    transaction.RollBack();  // ✅ 改为 RollBack
    continue;
}
```

---

### P1-7: Assembly.LoadFrom 锁定 DLL

**影响:** 运行时无法更新插件 DLL，需要重启 Revit

**文件:** `plugin/Core/CommandManager.cs:124`

**当前代码 (L122-124):**

```csharp
// 加载程序集
Assembly assembly = Assembly.LoadFrom(assemblyPath);
```

**修复方案：**

```csharp
// ✅ 使用 byte[] 加载，不锁定源文件
byte[] assemblyBytes = File.ReadAllBytes(assemblyPath);
Assembly assembly = Assembly.Load(assemblyBytes);
```

**注意：** `Assembly.Load(byte[])` 不会锁定源文件，但加载的程序集不能被卸载（.NET Framework 限制）。如果需要热更新，还需要结合 `AppDomain` 或使用 .NET 8 的 `AssemblyLoadContext`。

---

## P2 — 中风险（长期稳定性）

### P2-1: ManualResetEvent 泄漏

**影响:** 每个 EventHandler 中的 `ManualResetEvent` 从未 Dispose，长期运行泄漏内核句柄

**涉及文件:** 全部 83 个 EventHandler（~83 个 `new ManualResetEvent(false)`）

**解决思路——引入基类统一管理：**

**方案 A：创建基类 `WaitableEventHandler` 统一管理：**

```csharp
// 新建文件：commandset/Common/WaitableEventHandler.cs
public abstract class WaitableEventHandler : IExternalEventHandler, IWaitableExternalEventHandler, IDisposable
{
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
    private bool _disposed;

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _resetEvent?.Dispose();
            _disposed = true;
        }
    }

    // 子类在 Execute 的 finally 中调用此方法
    protected void SignalCompletion()
    {
        _resetEvent.Set();
    }

    // 子类在 SetParameters 中调用此方法
    protected void ResetSignal()
    {
        _resetEvent.Reset();
    }

    public abstract void Execute(UIApplication app);
    public abstract string GetName();
}
```

**方案 B（渐进式）——改 `WaitForCompletion` 使用 `using`：**

```csharp
// 每个 EventHandler 的 WaitForCompletion 改为：
public bool WaitForCompletion(int timeoutMilliseconds = 10000)
{
    using (ManualResetEvent resetEvent = _resetEvent) // 不会真的 dispose，仅演示
    {
        return resetEvent.WaitOne(timeoutMilliseconds);
    }
}
```

但是不能直接 Dispose `ManualResetEvent`，因为它在 Execute 中也被使用。**更实际的方案：** 在每个 EventHandler 的 `SetParameters()` 中确保信号量被正确重置，且最终被 `Dispose`。由于 EventHandler 实例复用，可以在 `ExternalEventCommandBase` 层面管理生命周期。

---

### P2-2: 空事务 Commit 替代 RollBack (MEP)

与 **P1-6** 相同，但该文件属于 MEP 子目录。修复同上。

---

### P2-3: 静态锁导致并发串行化

**影响:** 多客户端并发请求同一命令时串行执行，吞吐量降低

**文件:** `commandset/Commands/Delete/DeleteElementCommand.cs:10` (及其他命令文件)

**当前代码:**

```csharp
private static readonly object _executionLock = new object();
// ...
public override object Execute(JObject parameters, string requestId)
{
    lock (_executionLock) { ... }
}
```

**分析：** `ExternalEvent.Raise()` 本身保证同一命令的多次调用不会并发执行（Revit 最多队列一次 pending raise）。所以这个 `_executionLock` 其实是多余的。即使没有它，两个线程同时调 `RaiseAndWaitForCompletion` 也不会导致 Revit API 被并发访问。

**修复：**

```csharp
// ✅ 移除 static lock，或改为实例锁（如果需要）
public override object Execute(JObject parameters, string requestId)
{
    // 不需要 _executionLock，ExternalEvent 已保证串行
    try
    {
        var elementIds = parameters?["elementIds"]?.ToObject<string[]>();
        // ...
    }
    catch (Exception ex)
    {
        throw new Exception($"删除元素失败: {ex.Message}");
    }
}
```

---

### P2-4: InternalError 异常信息丢失

**影响:** 异常信息被包装为泛化消息，排查困难

**文件:** `commandset/Services/GetCurrentViewInfoEventHandler.cs:43`

**当前代码:**

```csharp
catch (Exception ex)
{
    TaskDialog.Show("error", "获取信息失败");  // ❌ 没有使用 ex.Message
}
```

**修复：**

```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"[GetCurrentViewInfo] Error: {ex}");
    Result = new ... { Message = $"获取信息失败: {ex.Message}" };
}
```

---

### P2-5: EventHandler 生命周期管理

**影响:** EventHandler 实例与命令对象耦合过紧，资源释放不明确

**当前模式:**

```csharp
// DeleteElementCommand.cs:8
public class DeleteElementCommand : ExternalEventCommandBase
{
    private DeleteElementEventHandler _handler => (DeleteElementEventHandler)Handler;
    // Handler 由 ExternalEventCommandBase 管理
}
```

`ExternalEventCommandBase` 创建的 `ExternalEvent` 通过 `ExternalEventManager.GetOrCreateEvent()` 缓存，以 Handler 类型名作为 key。这意味着**同一个 Handler 实例**在整个插件生命周期内被重复使用。

**这带来的问题：**
1. `_handler.ElementIds = ...` 在后台线程设置属性，然后在 UI 线程的 `Execute()` 中读取——没有内存屏障保证
2. 如果多个请求连续发送，后一个请求的 `SetParameters` 可能覆盖前一个尚未处理完的参数
3. `ManualResetEvent` 的 `Reset()` / `Set()` 时序问题

**修复：**

```csharp
// 在 ExternalEventCommandBase 中确保参数设置的原子性：
public abstract class ExternalEventCommandBase
{
    // 使用信号量避免并发设置参数
    private readonly SemaphoreSlim _paramGate = new SemaphoreSlim(1, 1);
    
    protected async Task SetParametersAsync(Action setAction)
    {
        await _paramGate.WaitAsync();
        try
        {
            setAction();
        }
        finally
        {
            _paramGate.Release();
        }
    }
}
```

或者更简单：在命令层面使用 `Interlocked` 或 `Volatile` 确保参数可见性。

---

## 系统性架构改进

参考 `revit-mcp-local-bridge` 的成熟实践，建议以下系统级改进：

### 1. FailurePreprocessor 机制

**参考:** `BridgeFailurePreprocessor.cs`

需求：注册全局 `IFailuresPreprocessor`，使所有事务在发生可恢复失败时自动处理，避免弹出 Revit 自带警告对话框阻塞命令执行。

```csharp
// 在 Transaction 开始时设置
FailureHandlingOptions failureOptions = transaction.GetFailureHandlingOptions();
failureOptions.SetFailuresPreprocessor(new RevitMCPFailurePreprocessor());
transaction.SetFailureHandlingOptions(failureOptions);

// FailurePreprocessor 实现
public class RevitMCPFailurePreprocessor : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        foreach (FailureMessageAccessor failure in failuresAccessor.GetFailureMessages())
        {
            if (failure.GetSeverity() == FailureSeverity.Warning)
            {
                failuresAccessor.DeleteWarning(failure);
                continue;
            }
            if (failure.GetSeverity() == FailureSeverity.Error ||
                failure.GetSeverity() == FailureSeverity.DocumentCorruption)
            {
                return FailureProcessingResult.ProceedWithRollBack;
            }
        }
        return FailureProcessingResult.Continue;
    }
}
```

### 2. 原子写入 IPC 文件

当前的 Socket 通信存在粘包问题（`stream.Read` 使用固定 8KB buffer）。建议参考 `BridgeFileQueue.WriteAtomically` 实现原子消息写入。

### 3. 启动恢复

参考 `BridgeFileQueue` 的恢复逻辑，在插件启动时自动清理中断的处理请求。

---

## 操作清单（按文件）

| 文件 | 优先级 | 修改内容 |
|------|--------|----------|
| `plugin/Core/SocketService.cs` | **P0** | 全部空 catch 加日志；ProcessJsonRPCRequest 添加 UI 线程调度保护 |
| `plugin/Core/ExternalEventManager.cs` | **P0** | ActiveUIDocument null 检查 |
| `plugin/Core/Application.cs` | **P1** | OnShutdown catch 加日志 |
| `plugin/Core/CommandManager.cs` | **P1** | Assembly.LoadFrom → Assembly.Load(byte[]) |
| `commandset/Services/LoadFamilyEventHandler.cs` | **P1** | LoadFamily 移到事务外 |
| `commandset/Services/CreatePointElementEventHandler.cs` | **P1** | 事务内 Regenerate 移除；FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/TagRoomsEventHandler.cs` | **P1** | 事务内 Regenerate 移除；FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/TagWallsEventHandler.cs` | **P1** | 事务内 Regenerate 移除；FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/CreateStructuralFramingSystemEventHandler.cs` | **P1** | 事务内 Regenerate 移除；TaskDialog 移除 |
| `commandset/Services/MEP/PlaceFamilyInstanceEventHandler.cs` | **P1** | transaction.Commit() → RollBack() |
| `commandset/Services/ColorSplashEventHandler.cs` | **P1** | 2 处 FilteredElementCollector 加 using |
| `commandset/Services/CreateGridEventHandler.cs` | **P1** | FilteredElementCollector 加 using |
| `commandset/Services/CreateLineElementEventHandler.cs` | **P1** | FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/CreateSurfaceElementEventHandler.cs` | **P1** | FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/GetAvailableFamilyTypesEventHandler.cs` | **P1** | FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/GetCurrentViewElementsEventHandler.cs` | **P1** | FilteredElementCollector 加 using；TaskDialog 移除 |
| `commandset/Services/GetCurrentViewInfoEventHandler.cs` | **P1** | TaskDialog 移除，异常信息输出 |
| `commandset/Services/GetSelectedElementsEventHandler.cs` | **P1** | TaskDialog 移除 |
| `commandset/Services/DeleteElementEventHandler.cs` | **P1** | 3 处 TaskDialog 移除 |
| `commandset/Services/SayHelloEventHandler.cs` | **P1** | TaskDialog 改为 Debug 输出 |
| `commandset/Services/OperateElementEventHandler.cs` | **P1** | FilteredElementCollector 加 using |
| `commandset/Services/AIElementFilterEventHandler.cs` | **P1** | 2 处 FilteredElementCollector 加 using |
| `commandset/Commands/Delete/DeleteElementCommand.cs` | **P2** | 移除 static lock |
| 全部 83 个 EventHandler | **P2** | ManualResetEvent Dispose 管理 |
| 全部 EventHandler | **P1** | Lazy property null 检查 (`uiDoc`/`doc`) |

---

**优先级建议的执行顺序：**

1. **P0-3** → `ExternalEventManager.cs`（一行 null 检查，收益最大）
2. **P1-4** → `SocketService.cs` + `Application.cs`（加日志，便于排查后续问题）
3. **P1-2** → `LoadFamilyEventHandler.cs`（事务外加载族）
4. **P1-3** → 4 个文件（事务内 Regenerate 移除）
5. **P1-5** → 13 处 TaskDialog 替换
6. **P1-1** → 15 个文件 FilteredElementCollector 加 using
7. **P1-6** → `PlaceFamilyInstanceEventHandler.cs` Commit → RollBack
8. **P1-7** → `CommandManager.cs` LoadFrom → Load(byte[])
9. **P0-2** → `SocketService.cs` 线程安全调度
10. **P2** → 剩余优化项
