# Core Abstractions - 核心抽象层规格

## Overview

核心抽象层是整个框架的基础，提供所有上层模块依赖的契约（接口和基础类型）。

## Requirements

### R-1: OperateResult 返回模型

#### R-1.1 基本返回模型

```csharp
public class OperateResult
{
    public bool IsSuccess { get; set; }
    public int ErrorCode { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
```

#### R-1.2 泛型返回模型

```csharp
public class OperateResult<T> : OperateResult
{
    public T? Content { get; set; }
}
```

#### R-1.3 工厂方法

- `OperateResult.Ok()` - 创建成功结果
- `OperateResult.Fail(int code, string msg)` - 创建失败结果
- `OperateResult<T>.Ok(T content)` - 创建带内容的成功结果
- `OperateResult<T>.Fail(OperateResult source)` - 从源结果复制失败

### R-2: ErrorCode 错误码规范

错误码分段管理，便于定位来源：

| 范围 | 用途 | 示例 |
|------|------|------|
| 0 | 成功 | Success |
| -1 ~ -99 | 通用错误 | Unknown, InvalidArgument, Timeout |
| 1xxx | 通讯 | CommNotConnected, CommReadFailed |
| 2xxx | 数据库 | DbConnectFailed, DbExecuteFailed |
| 3xxx | 权限 | AuthUnauthorized, AuthForbidden |
| 4xxx | 业务 | BizValidationFailed |
| 5xxx | 配置 | ConfigNotFound, ConfigInvalid |
| 6xxx | 脚本 | ScriptCompileError, ScriptRuntimeError |

### R-3: DI 容器抽象

#### R-3.1 IServiceRegistry

```csharp
public enum ServiceLifetime { Singleton, Scoped, Transient }

public interface IServiceRegistry
{
    void Register<TService, TImpl>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TImpl : class, TService;
    void Register<TService>(TService instance) where TService : class;
    void Register(Type service, Type implementation, ServiceLifetime lifetime);
}
```

#### R-3.2 IServiceResolver

```csharp
public interface IServiceResolver : IDisposable
{
    T Resolve<T>();
    T? TryResolve<T>();
    IEnumerable<T> ResolveAll<T>();
    IServiceResolver CreateScope();
}
```

### R-4: 模块系统

#### R-4.1 IModule 接口

```csharp
public interface IModule
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    IReadOnlyList<string> Dependencies { get; }
    void RegisterServices(IServiceRegistry services);
    Task OnInitializeAsync(IServiceResolver resolver, CancellationToken ct);
    void RegisterUI(IViewRegistry views, IMenuRegistry menus);
    Task OnShutdownAsync(CancellationToken ct);
}
```

#### R-4.2 模块加载策略

| 策略 | 行为 |
|------|------|
| Eager | 启动时同步加载 |
| Lazy | 首次访问时加载 |
| Preload | 后台异步预加载 |
| Manual | 仅显式调用时加载 |

### R-5: 插件系统

```csharp
public interface IPlugin : IDisposableAsync
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    Task<OperateResult> LoadAsync(IServiceResolver resolver, CancellationToken ct);
    Task<OperateResult> UnloadAsync(CancellationToken ct);
}
```

### R-6: 初始化接口

```csharp
public interface IInitializable
{
    Task InitializeAsync(CancellationToken ct = default);
}

public interface IDisposableAsync : IDisposable
{
    Task DisposeAsync();
}
```

---

## Acceptance Criteria

- [ ] OperateResult 支持链式调用
- [ ] ErrorCode 覆盖主要错误场景
- [ ] IServiceRegistry 支持泛型和非泛型注册
- [ ] IServiceResolver 支持作用域
- [ ] IModule 支持依赖声明
- [ ] IModule 支持多种加载策略
- [ ] IPlugin 支持热加载/卸载
- [ ] 所有接口提供 Async 版本

---

## Dependencies

无 - 此规格是所有其他规格的基础
