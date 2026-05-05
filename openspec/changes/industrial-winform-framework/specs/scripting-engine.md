# Scripting Engine - 脚本引擎规格

## Overview

脚本引擎提供运行时脚本执行能力，允许用户通过编写 C# 脚本来自定义业务逻辑。

## Requirements

### R-1: IScriptEngine 接口

```csharp
public interface IScriptEngine
{
    string Name { get; }
    Version Version { get; }

    ScriptValidationResult Validate(string scriptCode);
    ScriptCompilationResult Compile(string scriptCode, ScriptOptions? options = null);
    ScriptExecutionResult Execute(string scriptCode, IScriptContext context,
        ScriptOptions? options = null);
    Task<ScriptExecutionResult> ExecuteAsync(string scriptCode, IScriptContext context,
        ScriptOptions? options = null,
        CancellationToken ct = default);

    void Precompile(string scriptCode, string scriptId);
    ScriptCompilationResult? GetCached(string scriptId);
    void ClearCache();
}
```

### R-2: IScriptContext 上下文

```csharp
public interface IScriptContext
{
    T? GetGlobal<T>(string name) where T : class;
    void SetGlobal<T>(string name, T value) where T : class;
    IReadOnlyDictionary<string, object> GetAllGlobals();
    void RegisterMethod(string name, Delegate method);
    void WriteLine(string message);
    IReadOnlyList<string> GetOutput();
    void ClearOutput();
}
```

### R-3: IScriptManager 管理器

```csharp
public interface IScriptManager : IAsyncDisposable
{
    IReadOnlyList<ScriptDescriptor> GetAllScripts();
    ScriptDescriptor? GetScript(string scriptId);
    Task<OperateResult<ScriptDescriptor>> SaveScriptAsync(ScriptDescriptor script,
        CancellationToken ct = default);
    Task<OperateResult> DeleteScriptAsync(string scriptId, CancellationToken ct = default);
    Task<ScriptExecutionResult> ExecuteAsync(string scriptId,
        CancellationToken ct = default);
    Task<OperateResult> SetEnabledAsync(string scriptId, bool enabled,
        CancellationToken ct = default);
    IReadOnlyList<ScriptVersion> GetVersions(string scriptId);
    Task<OperateResult> RollbackAsync(string scriptId, int version,
        CancellationToken ct = default);

    event EventHandler<ScriptStateChangedEventArgs>? ScriptStateChanged;
}
```

### R-4: 脚本描述符

```csharp
public class ScriptDescriptor
{
    public string Id { get; init; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; set; }
    public int CurrentVersion { get; set; }
    public ScriptTrigger Trigger { get; set; }
    public string? CronExpression { get; set; }
}

public enum ScriptTrigger
{
    Manual,      // 手动触发
    Scheduled,   // 定时触发 (Cron)
    OnEvent,     // 事件触发
    OnTagChanged // 点位变化触发
}
```

### R-5: 执行结果

```csharp
public class ScriptValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ScriptError> Errors { get; init; }
    public IReadOnlyList<ScriptDiagnostic> Warnings { get; init; }
}

public class ScriptExecutionResult
{
    public bool IsSuccess { get; init; }
    public object? ReturnValue { get; init; }
    public IReadOnlyList<string> Output { get; init; }
    public IReadOnlyList<ScriptError> Errors { get; init; }
    public TimeSpan Elapsed { get; init; }
    public Exception? Exception { get; init; }
}
```

### R-6: 脚本选项

```csharp
public class ScriptOptions
{
    public int TimeoutMs { get; set; } = 30000;
    public bool AllowAwait { get; set; } = true;
    public bool AllowUnsafe { get; set; } = false;
    public bool FullStackTrace { get; set; } = false;
    public int MaxOutputLines { get; set; } = 1000;
    public int MemoryLimitMb { get; set; } = 128;
}
```

### R-7: IScriptSandbox 安全沙箱

```csharp
public interface IScriptSandbox
{
    SecurityCheckResult CheckCode(string code);
}

public class SecurityCheckResult
{
    public bool IsAllowed { get; init; }
    public string? Reason { get; init; }
}
```

**禁止的操作**：
- 访问文件系统 (除指定目录)
- 网络访问
- 进程/线程操作
- 反射访问私有成员
- 危险方法调用 (Process, Assembly.Load 等)

### R-8: IndustrialScriptGlobals 内置全局对象

```csharp
public class IndustrialScriptGlobals
{
    // 日志
    public void Log(string message);
    public void LogWarning(string message);
    public void LogError(string message);

    // 设备操作
    public IDeviceManager Devices { get; }
    public async Task<T?> ReadTagAsync<T>(string deviceId, string address);
    public async Task WriteTagAsync<T>(string deviceId, string address, T value);

    // 事件总线
    public IEventBus EventBus { get; }

    // 缓存
    public IAppCache Cache { get; }

    // 工具
    public DateTime Now { get; }
    public double Sin(double x);
    public double Cos(double x);
    public double Abs(double x);
    public int Min(int a, int b);
    public int Max(int a, int b);
    public double Round(double value, int digits = 0);
}
```

### R-9: 脚本编辑器控件

```csharp
public class ScriptEditorControl : UserControl
{
    public string Code { get; set; }
    public string? ScriptId { get; set; }
    public event EventHandler<ScriptValidationResult>? ValidationChanged;

    public void Validate();
    public async Task<ScriptExecutionResult> ExecuteAsync();
}
```

---

## Acceptance Criteria

- [ ] 支持 C# 脚本语法 (基于 Roslyn)
- [ ] 支持 async/await
- [ ] 支持语法验证和错误提示
- [ ] 支持脚本版本管理
- [ ] 支持多种触发方式 (手动/定时/事件/点位变化)
- [ ] 支持安全沙箱
- [ ] 内置点位读写、日志、缓存等全局对象
- [ ] 提供脚本编辑器控件
- [ ] 支持脚本输出捕获
- [ ] 支持超时和内存限制

---

## Dependencies

- `core-abstractions`
- `communication-layer`
