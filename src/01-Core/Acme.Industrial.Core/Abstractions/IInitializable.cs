namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 可初始化的接口。
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// 初始化。
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);
}

/// <summary>
/// 异步可释放接口 - 扩展 System 版本并添加异步释放语义。
/// </summary>
public interface IAsyncDisposable : System.IAsyncDisposable
{
    /// <summary>
    /// 异步释放。
    /// </summary>
    new ValueTask DisposeAsync();
}
