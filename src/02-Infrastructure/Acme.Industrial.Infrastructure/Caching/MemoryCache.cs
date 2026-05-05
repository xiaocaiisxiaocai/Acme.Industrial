using System.Collections.Concurrent;
using Acme.Industrial.Core.Caching;

namespace Acme.Industrial.Infrastructure.Caching;

/// <summary>
/// 内存缓存实现。
/// </summary>
public class MemoryCache : IAppCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly System.Threading.Timer _cleanupTimer;

    public MemoryCache()
    {
        _cleanupTimer = new System.Threading.Timer(Cleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public int Count => _cache.Count;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        await Task.Yield();
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.Now)
            {
                _cache.TryRemove(key, out _);
                return default;
            }
            if (entry.Value is T typed)
                return typed;
        }
        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await Task.Yield();
        var entry = new CacheEntry
        {
            Value = value!,
            ExpiresAt = ttl.HasValue ? DateTime.Now.Add(ttl.Value) : null
        };
        _cache[key] = entry;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await Task.Yield();
        _cache.TryRemove(key, out _);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        await Task.Yield();
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.Now)
            {
                _cache.TryRemove(key, out _);
                return false;
            }
            return true;
        }
        return false;
    }

    public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var existing = await GetAsync<T>(key, ct);
        if (existing != null)
            return existing;

        var value = await factory();
        await SetAsync(key, value, ttl, ct);
        return value;
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await Task.Yield();
        _cache.Clear();
    }

    private void Cleanup(object? state)
    {
        var now = DateTime.Now;
        foreach (var kvp in _cache.Where(x => x.Value.ExpiresAt.HasValue && x.Value.ExpiresAt.Value < now))
        {
            _cache.TryRemove(kvp.Key, out _);
        }
    }

    private class CacheEntry
    {
        public object Value { get; init; } = null!;
        public DateTime? ExpiresAt { get; init; }
    }
}
