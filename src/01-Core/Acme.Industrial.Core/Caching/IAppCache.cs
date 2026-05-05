namespace Acme.Industrial.Core.Caching;

/// <summary>
/// 应用程序缓存接口。
/// </summary>
public interface IAppCache
{
    /// <summary>
    /// 获取值。
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// 设置值。
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>
    /// 移除值。
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 检查键是否存在。
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 获取或添加值。
    /// </summary>
    Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>
    /// 清除所有缓存。
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取缓存项数量。
    /// </summary>
    int Count { get; }
}
