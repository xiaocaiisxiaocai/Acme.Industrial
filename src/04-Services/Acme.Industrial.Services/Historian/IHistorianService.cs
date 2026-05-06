namespace Acme.Industrial.Services.Historian;

/// <summary>
/// 查询模式
/// </summary>
public enum QueryMode
{
    /// <summary>原始数据</summary>
    Raw,
    /// <summary>趋势数据（等间隔采样）</summary>
    Trend,
    /// <summary>插值数据</summary>
    Interpolated
}

/// <summary>
/// 聚合类型
/// </summary>
public enum AggregationType
{
    Average, Min, Max, Sum, Count, First, Last, StdDev
}

/// <summary>
/// 历史数据点
/// </summary>
public class HistorianDataPoint
{
    /// <summary>点位名称</summary>
    public string TagName { get; init; } = string.Empty;

    /// <summary>值</summary>
    public object? Value { get; init; }

    /// <summary>数据质量</summary>
    public byte Quality { get; init; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// 历史数据查询条件
/// </summary>
public class HistorianQuery
{
    /// <summary>点位名称</summary>
    public string TagName { get; init; } = string.Empty;

    /// <summary>开始时间</summary>
    public DateTime StartTime { get; init; }

    /// <summary>结束时间</summary>
    public DateTime EndTime { get; init; }

    /// <summary>最大返回点数</summary>
    public int MaxPoints { get; set; } = 10000;

    /// <summary>查询模式</summary>
    public QueryMode Mode { get; set; } = QueryMode.Trend;
}

/// <summary>
/// 聚合查询条件
/// </summary>
public class AggregatedQuery
{
    /// <summary>点位名称</summary>
    public string TagName { get; init; } = string.Empty;

    /// <summary>开始时间</summary>
    public DateTime StartTime { get; init; }

    /// <summary>结束时间</summary>
    public DateTime EndTime { get; init; }

    /// <summary>聚合类型</summary>
    public AggregationType AggregationType { get; init; }

    /// <summary>聚合间隔</summary>
    public TimeSpan Interval { get; init; }
}

/// <summary>
/// 聚合数据点
/// </summary>
public class AggregatedDataPoint
{
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>聚合值</summary>
    public double Value { get; init; }

    /// <summary>最小值</summary>
    public double Min { get; init; }

    /// <summary>最大值</summary>
    public double Max { get; init; }

    /// <summary>样本数</summary>
    public int Count { get; init; }
}

/// <summary>
/// 历史数据统计
/// </summary>
public class HistorianStatistics
{
    /// <summary>总存储点数</summary>
    public long TotalPoints { get; init; }

    /// <summary>存储占用（字节）</summary>
    public long StorageBytes { get; init; }

    /// <summary>点位数量</summary>
    public int TagCount { get; init; }

    /// <summary>写入速率（点/秒）</summary>
    public double WriteRate { get; init; }
}

/// <summary>
/// 历史数据服务接口
/// </summary>
public interface IHistorianService : Acme.Industrial.Core.Abstractions.IInitializable, IDisposable
{
    /// <summary>写入单个数据点</summary>
    Task<Acme.Industrial.Core.Results.OperateResult> WriteAsync(HistorianDataPoint point, CancellationToken ct = default);

    /// <summary>批量写入数据点</summary>
    Task<Acme.Industrial.Core.Results.OperateResult> WriteBatchAsync(System.Collections.Generic.IEnumerable<HistorianDataPoint> points, CancellationToken ct = default);

    /// <summary>查询历史数据</summary>
    Task<System.Collections.Generic.IReadOnlyList<HistorianDataPoint>> QueryAsync(HistorianQuery query, CancellationToken ct = default);

    /// <summary>查询并转换历史数据</summary>
    Task<System.Collections.Generic.IReadOnlyList<T>> QueryAsync<T>(HistorianQuery query, System.Func<HistorianDataPoint, T> selector, CancellationToken ct = default);

    /// <summary>聚合查询</summary>
    Task<System.Collections.Generic.IReadOnlyList<AggregatedDataPoint>> QueryAggregatedAsync(AggregatedQuery query, CancellationToken ct = default);

    /// <summary>设置数据保留策略</summary>
    Task<Acme.Industrial.Core.Results.OperateResult> SetRetentionAsync(string tagName, TimeSpan retention, CancellationToken ct = default);

    /// <summary>获取数据存储大小</summary>
    Task<Acme.Industrial.Core.Results.OperateResult<long>> GetDataSizeAsync(string tagName, CancellationToken ct = default);

    /// <summary>获取统计信息</summary>
    Task<HistorianStatistics> GetStatisticsAsync(CancellationToken ct = default);
}
