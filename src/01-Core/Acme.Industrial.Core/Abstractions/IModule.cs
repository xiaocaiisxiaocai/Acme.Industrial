using Acme.Industrial.Core.UI;

namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 模块接口 - 所有业务模块需实现此接口。
/// </summary>
public interface IModule
{
    /// <summary>模块唯一 ID。</summary>
    string Id { get; }

    /// <summary>模块名称。</summary>
    string Name { get; }

    /// <summary>模块版本。</summary>
    Version Version { get; }

    /// <summary>依赖的其他模块 ID。</summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>加载策略。</summary>
    ModuleLoadPolicy LoadPolicy { get; }

    /// <summary>注册模块的服务到 DI 容器。</summary>
    void RegisterServices(IServiceRegistry services);

    /// <summary>容器构建完成后的初始化（可拿服务）。</summary>
    Task OnInitializeAsync(IServiceResolver resolver, CancellationToken ct);

    /// <summary>注册模块的菜单、视图。</summary>
    void RegisterUI(IViewRegistry views, IMenuRegistry menus);

    /// <summary>模块卸载。</summary>
    Task OnShutdownAsync(CancellationToken ct);
}

/// <summary>
/// 模块加载策略。
/// </summary>
public enum ModuleLoadPolicy
{
    /// <summary>启动时预加载（必须模块）。</summary>
    Eager,

    /// <summary>首次访问时加载（默认）。</summary>
    Lazy,

    /// <summary>后台预加载（启动后异步加载，不阻塞）。</summary>
    Preload,

    /// <summary>显式加载（仅在代码中显式调用）。</summary>
    Manual
}

/// <summary>
/// 模块元数据。
/// </summary>
public class ModuleMetadata
{
    /// <summary>模块唯一 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>模块显示名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>模块版本。</summary>
    public Version Version { get; init; } = new(1, 0, 0);

    /// <summary>入口类型全名。</summary>
    public string EntryType { get; init; } = string.Empty;

    /// <summary>程序集路径。</summary>
    public string AssemblyPath { get; init; } = string.Empty;

    /// <summary>依赖的其他模块 ID。</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>加载策略。</summary>
    public ModuleLoadPolicy LoadPolicy { get; init; } = ModuleLoadPolicy.Lazy;

    /// <summary>模块描述。</summary>
    public string? Description { get; init; }

    /// <summary>作者。</summary>
    public string? Author { get; init; }
}
