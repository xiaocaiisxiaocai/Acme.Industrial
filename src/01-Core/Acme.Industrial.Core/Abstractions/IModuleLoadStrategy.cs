using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 模块加载策略接口。
/// </summary>
public interface IModuleLoadStrategy
{
    /// <summary>
    /// 决定模块是否应该在启动时加载。
    /// </summary>
    bool ShouldLoadOnStartup(ModuleMetadata metadata);

    /// <summary>
    /// 加载模块实例。
    /// </summary>
    Task<IModule?> LoadAsync(ModuleMetadata metadata, ModuleLoadContext context, CancellationToken ct = default);
}

/// <summary>
/// 加载策略上下文。
/// </summary>
public class ModuleLoadContext
{
    /// <summary>模块注册中心。</summary>
    public required IModuleRegistry Registry { get; init; }

    /// <summary>服务解析器。</summary>
    public required IServiceResolver Services { get; init; }

    /// <summary>日志工厂。</summary>
    public IAppLoggerFactory LoggerFactory { get; init; } = null!;

    /// <summary>是否在启动阶段。</summary>
    public bool IsStartup { get; init; }

    /// <summary>已加载模块数量。</summary>
    public int LoadedCount { get; init; }
}
