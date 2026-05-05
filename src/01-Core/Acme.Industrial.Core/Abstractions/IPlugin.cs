using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 插件接口 - 运行时可加载/卸载的扩展。
/// </summary>
public interface IPlugin : IAsyncDisposable
{
    /// <summary>插件唯一 ID。</summary>
    string Id { get; }

    /// <summary>插件名称。</summary>
    string Name { get; }

    /// <summary>插件版本。</summary>
    Version Version { get; }

    /// <summary>
    /// 加载插件。
    /// </summary>
    Task<OperateResult> LoadAsync(IServiceResolver resolver, CancellationToken ct = default);

    /// <summary>
    /// 卸载插件。
    /// </summary>
    Task<OperateResult> UnloadAsync(CancellationToken ct = default);
}
