using System.Collections.Concurrent;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Serialization;

namespace Acme.Industrial.Infrastructure.Auditing;

/// <summary>
/// 审计操作类型。
/// </summary>
public enum AuditOperation
{
    Create,
    Update,
    Delete,
    Read,
    Execute,
    Login,
    Logout,
    Export,
    Import,
    Other
}

/// <summary>
/// 审计日志条目。
/// </summary>
public class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? MachineName { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public AuditOperation Operation { get; init; }
    public string? OperationName { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? Description { get; init; }
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public TimeSpan? Duration { get; init; }
    public Dictionary<string, object?> ExtraData { get; init; } = new();
}

/// <summary>
/// 审计日志服务接口。
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
    Task LogAsync(
        AuditOperation operation,
        string entityName,
        string? entityId,
        string? description = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? userId = null,
        AuditOperation? operation = null,
        int limit = 100,
        CancellationToken ct = default);
}

/// <summary>
/// 内存审计日志实现。
/// </summary>
public class InMemoryAuditLogger : IAuditLogger
{
    private readonly ConcurrentBag<AuditEntry> _entries = new();
    private readonly IAppLogger _logger;
    private readonly int _maxEntries;

    public InMemoryAuditLogger(IAppLogger logger, int maxEntries = 10000)
    {
        _logger = logger;
        _maxEntries = maxEntries;
    }

    public Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _entries.Add(entry);

        if (_entries.Count > _maxEntries)
        {
            TrimEntries();
        }

        _logger.Info($"审计日志: {entry.Operation} - {entry.EntityName} by {entry.UserName}");
        return Task.CompletedTask;
    }

    public Task LogAsync(
        AuditOperation operation,
        string entityName,
        string? entityId,
        string? description = null,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry
        {
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            MachineName = Environment.MachineName
        };
        return LogAsync(entry, ct);
    }

    public Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? userId = null,
        AuditOperation? operation = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        var query = _entries.AsEnumerable();

        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(e => e.UserId == userId);
        if (operation.HasValue)
            query = query.Where(e => e.Operation == operation.Value);

        var results = query
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditEntry>>(results);
    }

    private void TrimEntries()
    {
        var toRemove = _entries.Count - _maxEntries;
        if (toRemove <= 0) return;

        var entriesArray = _entries.ToArray();
        var sortedEntries = entriesArray
            .OrderBy(e => e.Timestamp)
            .Take(toRemove)
            .Select(e => e.Id)
            .ToHashSet();

        var remaining = new ConcurrentBag<AuditEntry>(
            entriesArray.Where(e => !sortedEntries.Contains(e.Id)));
    }
}

/// <summary>
/// 数据库审计日志实现。
/// </summary>
public class DatabaseAuditLogger : IAuditLogger
{
    private readonly Func<Task> _logAction;
    private readonly IAppLogger _logger;

    public DatabaseAuditLogger(IAppLogger logger, Func<Task> logAction)
    {
        _logger = logger;
        _logAction = logAction;
    }

    public Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _logAction();
                _logger.Debug($"审计日志已保存: {entry.Operation} - {entry.EntityName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"保存审计日志失败", ex);
            }
        }, ct);

        return Task.CompletedTask;
    }

    public Task LogAsync(
        AuditOperation operation,
        string entityName,
        string? entityId,
        string? description = null,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry
        {
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            Description = description
        };
        return LogAsync(entry, ct);
    }

    public Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? userId = null,
        AuditOperation? operation = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<AuditEntry>>(Array.Empty<AuditEntry>());
    }
}

/// <summary>
/// 文件审计日志实现。
/// </summary>
public class FileAuditLogger : IAuditLogger, IDisposable
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ISerializer _serializer;
    private readonly IAppLogger _logger;
    private bool _disposed;

    public FileAuditLogger(IAppLogger logger, string logPath, ISerializer serializer)
    {
        _logger = logger;
        _logPath = logPath;
        _serializer = serializer;

        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var line = _serializer.Serialize(entry);
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);
            _logger.Debug($"审计日志已保存: {entry.Operation} - {entry.EntityName}");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存审计日志失败", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task LogAsync(
        AuditOperation operation,
        string entityName,
        string? entityId,
        string? description = null,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry
        {
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            Description = description
        };
        return LogAsync(entry, ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? userId = null,
        AuditOperation? operation = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        if (!File.Exists(_logPath))
        {
            return Array.Empty<AuditEntry>();
        }

        var lines = await File.ReadAllLinesAsync(_logPath);
        var entries = new List<AuditEntry>();

        foreach (var line in lines.Reverse().Take(limit * 2))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = _serializer.Deserialize<AuditEntry>(line);
                if (entry == null) continue;

                if (from.HasValue && entry.Timestamp < from.Value) break;
                if (to.HasValue && entry.Timestamp > to.Value) continue;
                if (!string.IsNullOrEmpty(userId) && entry.UserId != userId) continue;
                if (operation.HasValue && entry.Operation != operation.Value) continue;

                entries.Add(entry);
                if (entries.Count >= limit) break;
            }
            catch { }
        }

        return entries;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }
}

/// <summary>
/// 审计日志帮助类。
/// </summary>
public static class AuditHelper
{
    public static AuditEntry CreateEntry(
        AuditOperation operation,
        string entityName,
        string? entityId = null,
        string? description = null)
    {
        return new AuditEntry
        {
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            UserId = GetCurrentUserId(),
            UserName = GetCurrentUserName(),
            IpAddress = GetCurrentIpAddress(),
            MachineName = Environment.MachineName
        };
    }

    public static AuditEntry CreateEntryWithValues(
        AuditOperation operation,
        string entityName,
        string? entityId,
        object? oldValue,
        object? newValue,
        ISerializer? serializer = null)
    {
        if (serializer == null)
        {
            var systemTextJsonType = Type.GetType("Acme.Industrial.Infrastructure.Serialization.SystemTextJsonSerializer, Acme.Industrial.Infrastructure");
            if (systemTextJsonType != null)
            {
                serializer = (ISerializer?)Activator.CreateInstance(systemTextJsonType);
            }
            else
            {
                throw new InvalidOperationException("未提供序列化器，且无法找到默认实现。请确保已引用 Infrastructure 项目。");
            }
        }

        return new AuditEntry
        {
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValue != null ? serializer.Serialize(oldValue) : null,
            NewValues = newValue != null ? serializer.Serialize(newValue) : null,
            UserId = GetCurrentUserId(),
            UserName = GetCurrentUserName(),
            IpAddress = GetCurrentIpAddress(),
            MachineName = Environment.MachineName
        };
    }

    public static string GetCurrentUserId() => "system";
    public static string GetCurrentUserName() => Environment.UserName;

    public static string GetCurrentIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
