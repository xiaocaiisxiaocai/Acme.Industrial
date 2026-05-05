namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 模块注册中心 - 负责管理模块元数据。
/// </summary>
public interface IModuleRegistry
{
    /// <summary>注册模块元数据。</summary>
    void Register(ModuleMetadata metadata);

    /// <summary>批量注册模块。</summary>
    void RegisterRange(IEnumerable<ModuleMetadata> modules);

    /// <summary>获取所有已注册模块。</summary>
    IReadOnlyList<ModuleMetadata> GetAllMetadata();

    /// <summary>获取模块元数据。</summary>
    ModuleMetadata? GetMetadata(string moduleId);

    /// <summary>检查模块是否已注册。</summary>
    bool IsRegistered(string moduleId);
}

/// <summary>
/// 模块状态变更事件参数。
/// </summary>
public class ModuleStateChangedEventArgs : EventArgs
{
    /// <summary>模块 ID。</summary>
    public string ModuleId { get; init; } = string.Empty;

    /// <summary>模块名称。</summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>旧状态。</summary>
    public ModuleState OldState { get; init; }

    /// <summary>新状态。</summary>
    public ModuleState NewState { get; init; }

    /// <summary>消息。</summary>
    public string? Message { get; init; }
}

/// <summary>
/// 模块状态。
/// </summary>
public enum ModuleState
{
    /// <summary>已扫描发现。</summary>
    Discovered,

    /// <summary>已注册。</summary>
    Registered,

    /// <summary>加载中。</summary>
    Loading,

    /// <summary>初始化中。</summary>
    Initializing,

    /// <summary>已加载。</summary>
    Loaded,

    /// <summary>活跃。</summary>
    Active,

    /// <summary>停用中。</summary>
    Deactivating,

    /// <summary>已卸载。</summary>
    Unloaded
}
