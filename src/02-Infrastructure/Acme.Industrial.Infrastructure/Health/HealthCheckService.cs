using System.Collections.Concurrent;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Infrastructure.Health;

/// <summary>
/// 健康检查状态。
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// 健康检查结果。
/// </summary>
public class HealthCheckResult
{
    public string Name { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public string? Message { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.Now;
    public Dictionary<string, object?> Tags { get; init; } = new();
}

/// <summary>
/// 整体健康报告。
/// </summary>
public class HealthReport
{
    public HealthStatus OverallStatus { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public Dictionary<string, HealthCheckResult> Results { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 健康检查器接口。
/// </summary>
public interface IHealthCheck
{
    string Name { get; }
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}

/// <summary>
/// 数据库健康检查。
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly Func<Task<bool>> _checkAction;
    private readonly string _connectionInfo;

    public string Name => "Database";

    public DatabaseHealthCheck(Func<Task<bool>> checkAction, string connectionInfo = "SQL Server")
    {
        _checkAction = checkAction;
        _connectionInfo = connectionInfo;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var isHealthy = await _checkAction();
            sw.Stop();

            return new HealthCheckResult
            {
                Name = Name,
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Message = isHealthy ? $"数据库连接正常: {_connectionInfo}" : "数据库连接失败",
                Duration = sw.Elapsed,
                Tags = new Dictionary<string, object?>
                {
                    ["connectionInfo"] = _connectionInfo
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult
            {
                Name = Name,
                Status = HealthStatus.Unhealthy,
                Message = $"数据库连接异常: {ex.Message}",
                Duration = sw.Elapsed,
                Tags = new Dictionary<string, object?>
                {
                    ["error"] = ex.Message,
                    ["connectionInfo"] = _connectionInfo
                }
            };
        }
    }
}

/// <summary>
/// 内存健康检查。
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _warningThresholdMB;
    private readonly long _criticalThresholdMB;

    public string Name => "Memory";

    public MemoryHealthCheck(long warningThresholdMB = 500, long criticalThresholdMB = 1000)
    {
        _warningThresholdMB = warningThresholdMB;
        _criticalThresholdMB = criticalThresholdMB;
    }

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024;
        sw.Stop();

        var status = workingSetMB >= _criticalThresholdMB
            ? HealthStatus.Unhealthy
            : workingSetMB >= _warningThresholdMB
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

        var message = status switch
        {
            HealthStatus.Healthy => $"内存使用正常: {workingSetMB} MB",
            HealthStatus.Degraded => $"内存使用偏高: {workingSetMB} MB",
            _ => $"内存使用过高: {workingSetMB} MB"
        };

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = status,
            Message = message,
            Duration = sw.Elapsed,
            Tags = new Dictionary<string, object?>
            {
                ["workingSetMB"] = workingSetMB,
                ["privateMemoryMB"] = privateMemoryMB,
                ["warningThresholdMB"] = _warningThresholdMB,
                ["criticalThresholdMB"] = _criticalThresholdMB
            }
        });
    }
}

/// <summary>
/// 磁盘空间健康检查。
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly string _drive;
    private readonly long _warningThresholdGB;
    private readonly long _criticalThresholdGB;

    public string Name => "DiskSpace";

    public DiskSpaceHealthCheck(
        string drive = "C",
        long warningThresholdGB = 10,
        long criticalThresholdGB = 5)
    {
        _drive = drive;
        _warningThresholdGB = warningThresholdGB;
        _criticalThresholdGB = criticalThresholdGB;
    }

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var driveInfo = new DriveInfo(_drive);
        var freeSpaceGB = driveInfo.AvailableFreeSpace / 1024 / 1024 / 1024;
        var totalSpaceGB = driveInfo.TotalSize / 1024 / 1024 / 1024;
        var usedPercent = (double)(totalSpaceGB - freeSpaceGB) / totalSpaceGB * 100;
        sw.Stop();

        var status = freeSpaceGB <= _criticalThresholdGB
            ? HealthStatus.Unhealthy
            : freeSpaceGB <= _warningThresholdGB
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = status,
            Message = $"磁盘空间: {freeSpaceGB} GB 可用 ({usedPercent:F1}% 已使用)",
            Duration = sw.Elapsed,
            Tags = new Dictionary<string, object?>
            {
                ["drive"] = _drive,
                ["freeSpaceGB"] = freeSpaceGB,
                ["totalSpaceGB"] = totalSpaceGB,
                ["usedPercent"] = usedPercent
            }
        });
    }
}

/// <summary>
/// 网络健康检查。
/// </summary>
public class NetworkHealthCheck : IHealthCheck
{
    private readonly string _url;
    private readonly int _timeoutMs;

    public string Name => "Network";

    public NetworkHealthCheck(string url = "https://www.google.com", int timeoutMs = 5000)
    {
        _url = url;
        _timeoutMs = timeoutMs;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(_timeoutMs) };
            var response = await client.GetAsync(_url, ct);
            sw.Stop();

            var status = response.IsSuccessStatusCode
                ? HealthStatus.Healthy
                : HealthStatus.Degraded;

            return new HealthCheckResult
            {
                Name = Name,
                Status = status,
                Message = $"网络连接正常 ({response.StatusCode})",
                Duration = sw.Elapsed,
                Tags = new Dictionary<string, object?>
                {
                    ["url"] = _url,
                    ["statusCode"] = (int)response.StatusCode
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult
            {
                Name = Name,
                Status = HealthStatus.Unhealthy,
                Message = $"网络连接失败: {ex.Message}",
                Duration = sw.Elapsed,
                Tags = new Dictionary<string, object?>
                {
                    ["url"] = _url,
                    ["error"] = ex.Message
                }
            };
        }
    }
}

/// <summary>
/// 健康检查服务。
/// </summary>
public class HealthCheckService : IDisposable
{
    private readonly ConcurrentDictionary<string, IHealthCheck> _checks = new();
    private readonly IAppLogger _logger;
    private bool _disposed;

    public event EventHandler<HealthReport>? HealthChanged;

    public HealthCheckService(IAppLogger logger)
    {
        _logger = logger;
    }

    public void Register(IHealthCheck healthCheck)
    {
        _checks[healthCheck.Name] = healthCheck;
        _logger.Debug($"健康检查器已注册: {healthCheck.Name}");
    }

    public void Register(string name, Func<CancellationToken, Task<HealthCheckResult>> checkFunc)
    {
        _checks[name] = new DelegateHealthCheck(name, checkFunc);
        _logger.Debug($"健康检查器已注册: {name}");
    }

    public void Unregister(string name)
    {
        _checks.TryRemove(name, out _);
        _logger.Debug($"健康检查器已移除: {name}");
    }

    public async Task<HealthReport> CheckAllAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new ConcurrentDictionary<string, HealthCheckResult>();

        var tasks = _checks.Select(async kvp =>
        {
            var result = await ExecuteCheckAsync(kvp.Value, ct);
            results[kvp.Key] = result;
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        var overallStatus = DetermineOverallStatus(results.Values);
        var report = new HealthReport
        {
            OverallStatus = overallStatus,
            TotalDuration = sw.Elapsed,
            Results = new Dictionary<string, HealthCheckResult>(results),
            ErrorMessage = overallStatus == HealthStatus.Unhealthy
                ? "一个或多个组件不健康"
                : null
        };

        HealthChanged?.Invoke(this, report);
        return report;
    }

    public async Task<HealthCheckResult> CheckAsync(string name, CancellationToken ct = default)
    {
        if (_checks.TryGetValue(name, out var check))
        {
            return await ExecuteCheckAsync(check, ct);
        }

        return new HealthCheckResult
        {
            Name = name,
            Status = HealthStatus.Unhealthy,
            Message = "健康检查器未找到",
            Duration = TimeSpan.Zero
        };
    }

    private async Task<HealthCheckResult> ExecuteCheckAsync(IHealthCheck check, CancellationToken ct)
    {
        try
        {
            return await check.CheckAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.Error($"健康检查执行失败: {check.Name}", ex);
            return new HealthCheckResult
            {
                Name = check.Name,
                Status = HealthStatus.Unhealthy,
                Message = $"检查执行失败: {ex.Message}",
                Duration = TimeSpan.Zero
            };
        }
    }

    private static HealthStatus DetermineOverallStatus(IEnumerable<HealthCheckResult> results)
    {
        var resultList = results.ToList();
        if (resultList.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;
        if (resultList.Any(r => r.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded;
        return HealthStatus.Healthy;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    private class DelegateHealthCheck : IHealthCheck
    {
        private readonly Func<CancellationToken, Task<HealthCheckResult>> _checkFunc;

        public string Name { get; }

        public DelegateHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> checkFunc)
        {
            Name = name;
            _checkFunc = checkFunc;
        }

        public Task<HealthCheckResult> CheckAsync(CancellationToken ct)
        {
            return _checkFunc(ct);
        }
    }
}
