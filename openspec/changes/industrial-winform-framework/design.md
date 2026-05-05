# 工业级 WinForm 框架 - 设计方案

## How

### 架构概览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Host (WinForm)                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │  Modules    │  │  Plugins    │  │  Scripting  │  │    UI      │       │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘       │
│         │                │                │                │              │
│  ┌──────┴────────────────┴────────────────┴────────────────┴──────┐      │
│  │                         Services Layer                            │      │
│  │  DataAcquisition │ Alarm │ Historian │ Recipe │ Motion │ Vision  │      │
│  └──────────────────────────┬──────────────────────────────────────┘      │
│                             │                                              │
│  ┌──────────────────────────┴──────────────────────────────────────┐      │
│  │                    Communication Layer                             │      │
│  │  IDeviceDriver │ ITagSubscriber │ IDeviceManager │ IDriverFactory│      │
│  │  Modbus │ S7 │ MC │ OPC UA │ Mock                                            │
│  └──────────────────────────┬──────────────────────────────────────┘      │
│                             │                                              │
│  ┌──────────────────────────┴──────────────────────────────────────┐      │
│  │                    Infrastructure Layer                            │      │
│  │  IAppLogger │ IAppConfiguration │ IAppCache │ IEventBus │ Data │      │
│  └──────────────────────────┬──────────────────────────────────────┘      │
│                             │                                              │
│  ┌──────────────────────────┴──────────────────────────────────────┐      │
│  │                      Core Layer                                  │      │
│  │  OperateResult │ IModule │ IPlugin │ IServiceRegistry │ DI     │      │
│  └─────────────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 依赖方向（单向引用）

```
Host ──► Modules ──► Services ──► Communication ──► Infrastructure ──► Core
          │                                                   ▲
          ├────► Scripting ────────────────────────────────┤
          │
          └────► UI ──► UI.Controls ◄────────────────────┘
```

**规则**：
1. Core 和 Common 禁止依赖任何业务层
2. Infrastructure 只能依赖 Core
3. 高层可依赖低层，低层不可依赖高层
4. Modules 之间禁止直接依赖，通过 EventBus 通讯

---

## 核心技术决策

### 1. 统一返回模型 (OperateResult)

**问题**：工业场景异常频繁（断线、超时），用异常处理会导致大量 try-catch。

**方案**：
```csharp
public class OperateResult<T>
{
    public bool IsSuccess { get; }
    public T? Content { get; }
    public int ErrorCode { get; }
    public string Message { get; }
    public Exception? Exception { get; }
}

public class OperateResult
{
    public static OperateResult Ok() => new() { IsSuccess = true };
    public static OperateResult Fail(int code, string msg) => new() { IsSuccess = false, ErrorCode = code, Message = msg };
}
```

**使用示例**：
```csharp
var result = await device.ReadAsync<float>("40001");
if (!result.IsSuccess)
{
    _logger.Error($"读取失败: {result.Message}");
    return;
}
Console.WriteLine($"温度 = {result.Content}");
```

### 2. 依赖注入 (Autofac)

**方案**：框架定义抽象，业务代码通过构造函数注入实现。

```csharp
// 框架定义接口
public interface IServiceRegistry
{
    void Register<TService, TImpl>(ServiceLifetime lifetime = ServiceLifetime.Singleton);
    T Resolve<T>();
}

public interface IServiceResolver
{
    T Resolve<T>();
    T? TryResolve<T>();
}
```

**Autofac 实现**：
```csharp
public class AutofacServiceContainer : IServiceRegistry, IServiceResolver
{
    private readonly ContainerBuilder _builder = new();
    private IContainer? _container;

    public void Register<TService, TImpl>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var rb = _builder.RegisterType<TImpl>().As<TService>();
        if (lifetime == ServiceLifetime.Singleton) rb.SingleInstance();
    }

    public T Resolve<T>() => _container!.Resolve<T>();
}
```

### 3. 设备驱动抽象 (IDeviceDriver)

**问题**：每个 PLC 型号协议不同，业务代码不应感知差异。

**方案**：
```csharp
public interface IDeviceDriver : IAsyncDisposable
{
    string DeviceId { get; }
    ConnectionState State { get; }
    Task<OperateResult> ConnectAsync(CancellationToken ct = default);
    Task<OperateResult<TagValue>> ReadAsync(Tag tag, CancellationToken ct = default);
    Task<OperateResult> WriteAsync(Tag tag, object value, CancellationToken ct = default);
    Task<OperateResult<IReadOnlyList<TagValue>>> ReadBatchAsync(IEnumerable<Tag> tags, CancellationToken ct = default);
}
```

**基类封装通用逻辑**：
```csharp
public abstract class DeviceDriverBase : IDeviceDriver
{
    // 连接状态管理、重试、统计、心跳、断线重连 - 所有驱动共用
    public async Task<OperateResult> ConnectAsync(CancellationToken ct = default)
    {
        // 重试逻辑、超时控制
        var result = await WithRetry(() => ConnectCoreAsync(ct), Options.RetryCount, ct);
        if (result.IsSuccess) StartHeartbeat();
        return result;
    }

    // 子类只需实现协议特定的 PDU 编解码
    protected abstract Task<OperateResult> ConnectCoreAsync(CancellationToken ct);
    protected abstract Task<OperateResult<byte[]>> ReadRawCoreAsync(string address, ushort length, CancellationToken ct);
}
```

### 4. 点位模型 (Tag/TagValue)

**方案**：
```csharp
public class Tag
{
    public string Name { get; init; }    // "Reactor1.Temperature"
    public string Address { get; init; }  // "40001" / "DB1.DBD0"
    public DataType DataType { get; init; }
    public int ScanRate { get; init; }     // 采集周期 ms
    public double Scale { get; init; } = 1.0;
    public double Offset { get; init; } = 0.0;
    public double DeadBand { get; init; }  // 死区
}

public class TagValue
{
    public string TagName { get; init; }
    public object? Value { get; init; }
    public TagQuality Quality { get; init; }  // Good/Bad/Uncertain
    public DateTime Timestamp { get; init; }
    public byte[]? RawBytes { get; init; }
}
```

### 5. 订阅式采集 (ITagSubscriber)

**问题**：点位多（1000+），轮询效率低。

**方案**：
```csharp
public interface ITagSubscriber : IDisposable
{
    IDisposable Subscribe(IEnumerable<Tag> tags, Action<TagValue> onValueChanged);
    void UnsubscribeAll();
}

// 实现：按 ScanRate 分组批量采集，变化超过 DeadBand 才回调
public class TagSubscriptionService : ITagSubscriber
{
    private async void OnScan(object? _)
    {
        var groups = _tags.GroupBy(t => t.ScanRate);
        foreach (var group in groups)
        {
            var result = await _driver.ReadBatchAsync(group);
            foreach (var tv in result.Content!)
                if (HasChanged(tv)) callback(tv);  // 死区过滤
        }
    }
}
```

### 6. 事件总线 (IEventBus)

**问题**：模块间耦合。

**方案**：
```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent;
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent;
}

// 使用
await _eventBus.PublishAsync(new AlarmTriggeredEvent { Level = AlarmLevel.High, Message = "温度过高" });
_eventBus.Subscribe<AlarmTriggeredEvent>(async (evt, ct) => await _alarmService.HandleAsync(evt));
```

### 7. 模块系统 (IModule)

**问题**：大型系统模块多，不可能全部同时加载。

**方案**：
```csharp
public interface IModule
{
    string Id { get; }
    IReadOnlyList<string> Dependencies { get; }
    void RegisterServices(IServiceRegistry services);
    Task OnInitializeAsync(IServiceResolver resolver, CancellationToken ct);
    void RegisterUI(IViewRegistry views, IMenuRegistry menus);
}

public enum ModuleLoadPolicy
{
    Eager,    // 启动时加载
    Lazy,     // 首次访问时加载
    Preload,  // 后台预加载
    Manual    // 显式加载
}
```

### 8. 脚本引擎 (IScriptEngine)

**问题**：用户需要自定义业务逻辑。

**方案**：
```csharp
public interface IScriptEngine
{
    ScriptValidationResult Validate(string code);
    ScriptExecutionResult Execute(string code, IScriptContext context);
}

public interface IScriptContext
{
    void SetGlobal(string name, object value);
    T? GetGlobal<T>(string name);
    void RegisterMethod(string name, Delegate method);
}

// 注入点位访问能力
public class IndustrialScriptGlobals
{
    public IDeviceManager Devices { get; }
    public IEventBus EventBus { get; }

    public async Task<T?> ReadTagAsync<T>(string deviceId, string address)
    {
        var device = Devices.GetDevice(deviceId);
        var result = await device.ReadAsync<T>(address);
        return result.Content;
    }
}
```

### 9. Repository 模式 (IRepository)

**问题**：数据库访问需要统一抽象。

**方案**：
```csharp
public interface IRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}

// SqlSugar 实现
public class SqlSugarRepository<TEntity, TKey> : IRepository<TEntity, TKey>
{
    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> pred, CancellationToken ct)
        => await _db.Queryable<TEntity>().Where(pred).ToListAsync(ct);
}
```

---

## 项目组织

### 解决方案结构

```
Acme.Industrial/
├── 00-Solution Items/
│   ├── Directory.Build.props      # 全局 MSBuild 属性
│   ├── Directory.Packages.props   # 集中包版本管理 (CPM)
│   └── .editorconfig             # 代码风格
│
├── 01-Core/
│   ├── Acme.Industrial.Core       # 核心抽象
│   ├── Acme.Industrial.Common    # 公共工具
│   └── Acme.Industrial.Domain    # 领域模型
│
├── 02-Infrastructure/
│   ├── Acme.Industrial.Logging   # Serilog 实现
│   ├── Acme.Industrial.Caching   # 内存缓存
│   └── Acme.Industrial.Data      # SqlSugar 实现
│
├── 03-Communication/
│   ├── Acme.Industrial.Communication.Abstractions
│   ├── Acme.Industrial.Communication.Core
│   ├── Acme.Industrial.Communication.Modbus
│   ├── Acme.Industrial.Communication.Siemens
│   └── Acme.Industrial.Communication.Mock
│
├── 04-Services/
│   ├── Acme.Industrial.Services.DataAcquisition
│   ├── Acme.Industrial.Services.Alarm
│   └── Acme.Industrial.Services.Historian
│
├── 05-UI/
│   ├── Acme.Industrial.UI
│   ├── Acme.Industrial.UI.Controls
│   └── Acme.Industrial.UI.Mvp
│
├── 09-Host/
│   └── Acme.Industrial.Host
│
└── 12-Tests/
    └── Acme.Industrial.Core.Tests
```

### NuGet 包版本管理

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Autofac" Version="8.0.0" />
    <PackageVersion Include="Serilog" Version="3.1.1" />
    <PackageVersion Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageVersion Include="SqlSugarCore" Version="5.1.4.158" />
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="4.8.0" />
    <PackageVersion Include="xunit" Version="2.6.5" />
    <PackageVersion Include="Moq" Version="4.20.70" />
  </ItemGroup>
</Project>
```

---

## 关键设计原则

| 原则 | 说明 |
|------|------|
| **接口先行** | 每个能力先定义接口，再做实现，方便替换与测试 |
| **OperateResult 优先** | 可预期的失败用返回值，不可预期的才抛异常 |
| **异步到底** | 所有 IO/通讯/数据库操作均提供 Async 版本 |
| **依赖抽象** | 业务代码不依赖 Serilog/Autofac/SqlSugar 等具体实现 |
| **批量优化** | 支持批量读写，减少通讯次数 |
| **Mock 优先** | 提供 Mock 驱动，便于无设备开发和 CI 测试 |

---

## 命名规范

### 项目命名

```
{公司}.{产品线}.{模块}.{子模块}
Acme.Industrial.Core
Acme.Industrial.Communication.Modbus
```

### 后缀约定

| 后缀 | 含义 | 示例 |
|------|------|------|
| `.Core` | 核心抽象 | `Acme.Industrial.Core` |
| `.Common` | 公共工具 | `Acme.Industrial.Common` |
| `.Abstractions` | 仅接口 | `Acme.Industrial.Communication.Abstractions` |
| `.Infrastructure` | 实现 | `Acme.Industrial.Infrastructure` |
| `.UI` | UI 框架 | `Acme.Industrial.UI` |
| `.Controls` | 控件库 | `Acme.Industrial.UI.Controls` |
| `.Modules.{XXX}` | 业务模块 | `Acme.Industrial.Modules.DeviceManagement` |
| `.Tests` | 测试 | `Acme.Industrial.Core.Tests` |
