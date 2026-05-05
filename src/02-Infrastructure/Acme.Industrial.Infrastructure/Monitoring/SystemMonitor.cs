using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Infrastructure.Monitoring;

/// <summary>
/// 系统监控指标。
/// </summary>
public class SystemMetrics
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public double CpuUsagePercent { get; init; }
    public long MemoryUsedMB { get; init; }
    public long MemoryTotalMB { get; init; }
    public double MemoryUsagePercent => MemoryTotalMB > 0
        ? (double)MemoryUsedMB / MemoryTotalMB * 100
        : 0;
    public long DiskReadBytesPerSec { get; init; }
    public long DiskWriteBytesPerSec { get; init; }
    public long NetworkSentBytesPerSec { get; init; }
    public long NetworkReceivedBytesPerSec { get; init; }
    public int ThreadCount { get; init; }
    public int HandleCount { get; init; }
    public TimeSpan UpTime { get; init; }
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
}

/// <summary>
/// 性能计数器信息。
/// </summary>
public class PerformanceCounterInfo
{
    public string CategoryName { get; init; } = string.Empty;
    public string CounterName { get; init; } = string.Empty;
    public string? InstanceName { get; init; }
    public double Value { get; init; }
}

/// <summary>
/// 系统监控服务。
/// </summary>
public class SystemMonitor : IDisposable
{
    private readonly IAppLogger _logger;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly ConcurrentDictionary<string, PerformanceCounter> _customCounters = new();
    private readonly System.Threading.Timer _timer;
    private readonly ConcurrentQueue<SystemMetrics> _metricsHistory;
    private readonly int _maxHistorySize;
    private bool _disposed;

    public event EventHandler<SystemMetrics>? MetricsCollected;

    public SystemMonitor(IAppLogger logger, int maxHistorySize = 3600)
    {
        _logger = logger;
        _maxHistorySize = maxHistorySize;
        _metricsHistory = new ConcurrentQueue<SystemMetrics>();

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            _cpuCounter.NextValue();
        }
        catch (Exception ex)
        {
            _logger.Warn($"性能计数器初始化失败: {ex.Message}");
            _cpuCounter = null;
            _ramCounter = null;
        }

        _timer = new System.Threading.Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public SystemMetrics GetCurrentMetrics()
    {
        return CollectCurrentMetrics();
    }

    public IReadOnlyList<SystemMetrics> GetHistory(int count = 60)
    {
        return _metricsHistory.TakeLast(count).ToList();
    }

    public void AddCustomCounter(string categoryName, string counterName, string? instanceName = null)
    {
        try
        {
            var key = $"{categoryName}|{counterName}|{instanceName}";
            if (!_customCounters.ContainsKey(key))
            {
                var counter = new PerformanceCounter(categoryName, counterName, instanceName ?? "", true);
                _customCounters[key] = counter;
                counter.NextValue();
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"添加性能计数器失败: {categoryName}/{counterName}", ex);
        }
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            var metrics = CollectCurrentMetrics();
            _metricsHistory.Enqueue(metrics);

            while (_metricsHistory.Count > _maxHistorySize)
            {
                _metricsHistory.TryDequeue(out _);
            }

            MetricsCollected?.Invoke(this, metrics);
        }
        catch (Exception ex)
        {
            _logger.Error($"收集系统指标失败", ex);
        }
    }

    private SystemMetrics CollectCurrentMetrics()
    {
        var process = Process.GetCurrentProcess();
        var cpuUsage = _cpuCounter?.NextValue() ?? 0;
        var ramAvailableMB = (long)(_ramCounter?.NextValue() ?? 0);
        var ramTotalMB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024;
        var ramUsedMB = ramTotalMB - ramAvailableMB;

        var customMetrics = new Dictionary<string, double>();
        foreach (var (key, counter) in _customCounters)
        {
            try
            {
                customMetrics[key] = counter.NextValue();
            }
            catch { }
        }

        return new SystemMetrics
        {
            Timestamp = DateTime.Now,
            CpuUsagePercent = cpuUsage,
            MemoryUsedMB = ramUsedMB,
            MemoryTotalMB = ramTotalMB,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            UpTime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            CustomMetrics = customMetrics
        };
    }

    public async Task<Dictionary<string, PerformanceCounterInfo>> GetAllCountersAsync(string categoryName)
    {
        var result = new Dictionary<string, PerformanceCounterInfo>();

        await Task.Run(() =>
        {
            try
            {
                var category = new PerformanceCounterCategory(categoryName);
                var instanceNames = category.GetInstanceNames();

                foreach (var instanceName in instanceNames)
                {
                    var counters = category.GetCounters(instanceName);
                    foreach (var counter in counters)
                    {
                        try
                        {
                            result[$"{instanceName}|{counter.CounterName}"] = new PerformanceCounterInfo
                            {
                                CategoryName = categoryName,
                                CounterName = counter.CounterName,
                                InstanceName = instanceName,
                                Value = counter.NextValue()
                            };
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"获取性能计数器失败: {categoryName}", ex);
            }
        });

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer.Dispose();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();

        foreach (var counter in _customCounters.Values)
        {
            counter.Dispose();
        }

        _customCounters.Clear();
    }
}

/// <summary>
/// 进程监控服务。
/// </summary>
public class ProcessMonitor : IDisposable
{
    private readonly IAppLogger _logger;
    private readonly Dictionary<int, Process> _monitoredProcesses = new();
    private readonly System.Threading.Timer _timer;
    private bool _disposed;

    public event EventHandler<ProcessInfo>? ProcessStarted;
    public event EventHandler<ProcessInfo>? ProcessStopped;

    public ProcessMonitor(IAppLogger logger)
    {
        _logger = logger;
        _timer = new System.Threading.Timer(CheckProcesses, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public void MonitorProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            _monitoredProcesses[processId] = process;
            _logger.Info($"开始监控进程: PID={processId}, Name={process.ProcessName}");
        }
        catch (Exception ex)
        {
            _logger.Error($"监控进程失败: PID={processId}", ex);
        }
    }

    public void MonitorProcess(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        foreach (var process in processes)
        {
            _monitoredProcesses[process.Id] = process;
        }
        _logger.Info($"开始监控进程: Name={processName}, Count={processes.Length}");
    }

    public void UnmonitorProcess(int processId)
    {
        if (_monitoredProcesses.Remove(processId))
        {
            _logger.Info($"停止监控进程: PID={processId}");
        }
    }

    public IReadOnlyList<ProcessInfo> GetMonitoredProcesses()
    {
        return _monitoredProcesses.Select(kvp => new ProcessInfo
        {
            Id = kvp.Key,
            Name = kvp.Value.ProcessName,
            WorkingSet64 = kvp.Value.WorkingSet64,
            PrivateMemorySize64 = kvp.Value.PrivateMemorySize64,
            TotalProcessorTime = kvp.Value.TotalProcessorTime,
            StartTime = kvp.Value.StartTime,
            Responding = kvp.Value.Responding
        }).ToList();
    }

    private void CheckProcesses(object? state)
    {
        var stoppedIds = new List<int>();

        foreach (var (pid, process) in _monitoredProcesses)
        {
            try
            {
                if (process.HasExited)
                {
                    stoppedIds.Add(pid);
                    ProcessStopped?.Invoke(this, new ProcessInfo
                    {
                        Id = pid,
                        Name = process.ProcessName
                    });
                    _logger.Warn($"监控的进程已退出: PID={pid}, Name={process.ProcessName}");
                }
            }
            catch { }
        }

        foreach (var id in stoppedIds)
        {
            _monitoredProcesses.Remove(id);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
        _monitoredProcesses.Clear();
    }
}

/// <summary>
/// 进程信息。
/// </summary>
public class ProcessInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public long WorkingSet64 { get; init; }
    public long PrivateMemorySize64 { get; init; }
    public TimeSpan TotalProcessorTime { get; init; }
    public DateTime StartTime { get; init; }
    public bool Responding { get; init; }
    public double CpuUsagePercent { get; init; }
}

/// <summary>
/// 应用性能监控。
/// </summary>
public class AppMetricsMonitor : IDisposable
{
    private readonly ConcurrentDictionary<string, MethodMetrics> _methodMetrics = new();
    private readonly ConcurrentDictionary<string, RequestMetrics> _requestMetrics = new();
    private readonly IAppLogger _logger;
    private bool _disposed;

    public AppMetricsMonitor(IAppLogger logger)
    {
        _logger = logger;
    }

    public IDisposable TrackRequest(string endpoint)
    {
        return new RequestTracker(endpoint, this);
    }

    internal void RecordRequest(string endpoint, long durationMs, bool success)
    {
        _requestMetrics.AddOrUpdate(
            endpoint,
            _ => new RequestMetrics
            {
                Endpoint = endpoint,
                TotalRequests = 1,
                SuccessfulRequests = success ? 1 : 0,
                FailedRequests = success ? 0 : 1,
                AverageDurationMs = durationMs,
                MinDurationMs = durationMs,
                MaxDurationMs = durationMs
            },
            (_, m) =>
            {
                m.TotalRequests++;
                if (success) m.SuccessfulRequests++;
                else m.FailedRequests++;

                m.AverageDurationMs = (m.AverageDurationMs * (m.TotalRequests - 1) + durationMs) / m.TotalRequests;
                m.MinDurationMs = Math.Min(m.MinDurationMs, durationMs);
                m.MaxDurationMs = Math.Max(m.MaxDurationMs, durationMs);

                return m;
            });
    }

    public IReadOnlyList<RequestMetrics> GetRequestMetrics()
    {
        return _requestMetrics.Values.ToList();
    }

    public RequestMetrics? GetRequestMetrics(string endpoint)
    {
        return _requestMetrics.TryGetValue(endpoint, out var metrics) ? metrics : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _methodMetrics.Clear();
        _requestMetrics.Clear();
    }

    private class RequestTracker : IDisposable
    {
        private readonly string _endpoint;
        private readonly AppMetricsMonitor _monitor;
        private readonly Stopwatch _sw;

        public RequestTracker(string endpoint, AppMetricsMonitor monitor)
        {
            _endpoint = endpoint;
            _monitor = monitor;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            _monitor.RecordRequest(_endpoint, _sw.ElapsedMilliseconds, true);
        }
    }
}

/// <summary>
/// 请求指标。
/// </summary>
public class RequestMetrics
{
    public string Endpoint { get; init; } = string.Empty;
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public double SuccessRate => TotalRequests > 0
        ? (double)SuccessfulRequests / TotalRequests * 100
        : 0;
}

/// <summary>
/// 方法指标。
/// </summary>
public class MethodMetrics
{
    public string MethodName { get; init; } = string.Empty;
    public long CallCount { get; set; }
    public long TotalDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public double AverageDurationMs => CallCount > 0
        ? (double)TotalDurationMs / CallCount
        : 0;
}
