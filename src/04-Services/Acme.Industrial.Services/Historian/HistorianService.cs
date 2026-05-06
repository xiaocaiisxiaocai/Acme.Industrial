using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Acme.Industrial.Services.Historian;

/// <summary>
/// 历史数据服务实现
/// </summary>
public class HistorianService : IHistorianService
{
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, TagHistory> _tagHistories = new();
    private readonly ConcurrentDictionary<string, TimeSpan> _retentionPolicies = new();
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private long _totalWrites;
    private bool _disposed;

    public HistorianService(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.Info("历史数据服务初始化完成");
        return Task.CompletedTask;
    }

    public Task<OperateResult> WriteAsync(HistorianDataPoint point, CancellationToken ct = default)
    {
        if (point == null)
            return Task.FromResult(OperateResult.Fail(-1, "数据点不能为空"));

        try
        {
            var history = _tagHistories.GetOrAdd(point.TagName, _ => new TagHistory(point.TagName));
            history.Add(point);
            Interlocked.Increment(ref _totalWrites);

            return Task.FromResult(OperateResult.Ok());
        }
        catch (Exception ex)
        {
            _logger.Error("写入历史数据失败: " + point.TagName + " - " + ex.Message, ex);
            return Task.FromResult(OperateResult.Fail(-1, ex.Message));
        }
    }

    public Task<OperateResult> WriteBatchAsync(IEnumerable<HistorianDataPoint> points, CancellationToken ct = default)
    {
        var pointList = points.ToList();
        var successCount = 0;

        foreach (var point in pointList)
        {
            var history = _tagHistories.GetOrAdd(point.TagName, _ => new TagHistory(point.TagName));
            history.Add(point);
            successCount++;
        }

        Interlocked.Add(ref _totalWrites, successCount);
        _logger.Info("批量写入 " + successCount + " 个历史数据点");

        return Task.FromResult(OperateResult.Ok());
    }

    public Task<IReadOnlyList<HistorianDataPoint>> QueryAsync(HistorianQuery query, CancellationToken ct = default)
    {
        if (!_tagHistories.TryGetValue(query.TagName, out var history))
            return Task.FromResult<IReadOnlyList<HistorianDataPoint>>(Array.Empty<HistorianDataPoint>());

        var points = history.Query(query);
        return Task.FromResult<IReadOnlyList<HistorianDataPoint>>(points);
    }

    public Task<IReadOnlyList<T>> QueryAsync<T>(HistorianQuery query, Func<HistorianDataPoint, T> selector, CancellationToken ct = default)
    {
        var points = QueryAsync(query, ct).Result;
        var result = points.Select(selector).ToList();
        return Task.FromResult<IReadOnlyList<T>>(result);
    }

    public Task<IReadOnlyList<AggregatedDataPoint>> QueryAggregatedAsync(AggregatedQuery query, CancellationToken ct = default)
    {
        if (!_tagHistories.TryGetValue(query.TagName, out var history))
            return Task.FromResult<IReadOnlyList<AggregatedDataPoint>>(Array.Empty<AggregatedDataPoint>());

        var points = history.QueryAggregated(query);
        return Task.FromResult<IReadOnlyList<AggregatedDataPoint>>(points);
    }

    public Task<OperateResult> SetRetentionAsync(string tagName, TimeSpan retention, CancellationToken ct = default)
    {
        _retentionPolicies[tagName] = retention;

        if (_tagHistories.TryGetValue(tagName, out var history))
        {
            history.ApplyRetention(retention);
        }

        return Task.FromResult(OperateResult.Ok());
    }

    public Task<OperateResult<long>> GetDataSizeAsync(string tagName, CancellationToken ct = default)
    {
        if (_tagHistories.TryGetValue(tagName, out var history))
        {
            return Task.FromResult(OperateResult<long>.Ok(history.EstimatedSizeBytes));
        }

        return Task.FromResult(OperateResult<long>.Ok(0L));
    }

    public Task<HistorianStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var statistics = new HistorianStatistics
        {
            TotalPoints = _totalWrites,
            TagCount = _tagHistories.Count,
            WriteRate = _uptime.Elapsed.TotalSeconds > 0 ? _totalWrites / _uptime.Elapsed.TotalSeconds : 0
        };

        return Task.FromResult(statistics);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var history in _tagHistories.Values)
        {
            history.Dispose();
        }
        _tagHistories.Clear();

        GC.SuppressFinalize(this);
    }

    private class TagHistory : IDisposable
    {
        private readonly string _tagName;
        private readonly ConcurrentQueue<HistorianDataPoint> _data = new();
        private long _estimatedSize;

        public TagHistory(string tagName)
        {
            _tagName = tagName;
        }

        public long EstimatedSizeBytes => _estimatedSize;

        public void Add(HistorianDataPoint point)
        {
            _data.Enqueue(point);
            Interlocked.Add(ref _estimatedSize, EstimatePointSize(point));

            while (_data.Count > 1_000_000)
            {
                _data.TryDequeue(out _);
            }
        }

        public List<HistorianDataPoint> Query(HistorianQuery query)
        {
            return _data
                .Where(p => p.Timestamp >= query.StartTime && p.Timestamp <= query.EndTime)
                .TakeLast(query.MaxPoints)
                .ToList();
        }

        public List<AggregatedDataPoint> QueryAggregated(AggregatedQuery query)
        {
            var points = _data
                .Where(p => p.Timestamp >= query.StartTime && p.Timestamp <= query.EndTime)
                .OrderBy(p => p.Timestamp)
                .ToList();

            if (points.Count == 0)
                return new List<AggregatedDataPoint>();

            var result = new List<AggregatedDataPoint>();

            for (var time = query.StartTime; time <= query.EndTime; time += query.Interval)
            {
                var windowEnd = time + query.Interval;
                var window = points
                    .Where(p => p.Timestamp >= time && p.Timestamp < windowEnd)
                    .ToList();

                if (window.Count > 0)
                {
                    var values = window
                        .Where(p => p.Value is double or int or float)
                        .Select(p => Convert.ToDouble(p.Value))
                        .ToList();

                    if (values.Count > 0)
                    {
                        var aggregated = new AggregatedDataPoint
                        {
                            Timestamp = time,
                            Count = values.Count,
                            Min = values.Min(),
                            Max = values.Max(),
                            Value = query.AggregationType switch
                            {
                                AggregationType.Average => values.Average(),
                                AggregationType.Min => values.Min(),
                                AggregationType.Max => values.Max(),
                                AggregationType.Sum => values.Sum(),
                                AggregationType.Count => values.Count,
                                AggregationType.First => values.First(),
                                AggregationType.Last => values.Last(),
                                AggregationType.StdDev => CalculateStdDev(values),
                                _ => values.Average()
                            }
                        };
                        result.Add(aggregated);
                    }
                }
            }

            return result;
        }

        public void ApplyRetention(TimeSpan retention)
        {
            var cutoff = DateTime.Now - retention;
            while (_data.TryPeek(out var oldest) && oldest.Timestamp < cutoff)
            {
                _data.TryDequeue(out _);
            }
        }

        private static long EstimatePointSize(HistorianDataPoint point)
        {
            return 64 + (point.Value?.ToString()?.Length ?? 0) * 2;
        }

        private static double CalculateStdDev(List<double> values)
        {
            if (values.Count <= 1) return 0;
            var avg = values.Average();
            var sum = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sum / values.Count);
        }

        public void Dispose()
        {
            _data.Clear();
        }
    }
}
