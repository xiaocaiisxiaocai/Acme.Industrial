using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 模块生命周期管理器接口。
/// </summary>
public interface IModuleManager : IAsyncDisposable
{
    /// <summary>获取已加载模块。</summary>
    IModule? GetModule(string moduleId);

    /// <summary>获取所有已加载模块。</summary>
    IReadOnlyList<IModule> GetLoadedModules();

    /// <summary>检查模块是否已加载。</summary>
    bool IsLoaded(string moduleId);

    /// <summary>加载模块。</summary>
    Task<OperateResult<IModule>> LoadModuleAsync(string moduleId, CancellationToken ct = default);

    /// <summary>卸载模块。</summary>
    Task<OperateResult> UnloadModuleAsync(string moduleId, CancellationToken ct = default);

    /// <summary>预加载所有预加载策略模块。</summary>
    Task PreloadAllAsync(CancellationToken ct = default);

    /// <summary>模块状态变更事件。</summary>
    event EventHandler<ModuleStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// 模块实例包装。
/// </summary>
public class ModuleInstance : IDisposable
{
    /// <summary>元数据。</summary>
    public ModuleMetadata Metadata { get; }

    /// <summary>模块实例。</summary>
    public IModule Module { get; }

    /// <summary>加载时间。</summary>
    public DateTime LoadedAt { get; } = DateTime.Now;

    /// <summary>当前状态。</summary>
    public ModuleState State { get; private set; } = ModuleState.Loaded;

    public ModuleInstance(ModuleMetadata metadata, IModule module)
    {
        Metadata = metadata;
        Module = module;
    }

    /// <summary>
    /// 设置状态。
    /// </summary>
    public void SetState(ModuleState newState)
    {
        State = newState;
    }

    /// <summary>
    /// 释放资源。
    /// </summary>
    public void Dispose()
    {
        if (Module is IDisposable d)
            d.Dispose();
    }
}
