# Business Services - 业务服务层规格

## Overview

业务服务层提供工业场景常用的核心服务，包括数据采集、报警、历史数据等。

## Requirements

### R-1: IDataAcquisitionService 数据采集服务

```csharp
public interface IDataAcquisitionService : IInitializable, IDisposable
{
    // 点位订阅
    IDisposable Subscribe(IEnumerable<Tag> tags, Action<TagValue> callback);
    IDisposable Subscribe(string deviceId, IEnumerable<string> addresses, Action<TagValue> callback);
    
    // 批量读写
    Task<Dictionary<string, TagValue>> ReadBatchAsync(
        string deviceId, IEnumerable<string> addresses, CancellationToken ct = default);
    
    // 单点读写
    Task<OperateResult<TagValue>> ReadAsync(string deviceId, string address, 
        CancellationToken ct = default);
    Task<OperateResult> WriteAsync(string deviceId, string address, object value,
        CancellationToken ct = default);
    
    // 设备管理
    Task<OperateResult> ConnectDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<OperateResult> DisconnectDeviceAsync(string deviceId, CancellationToken ct = default);
    ConnectionState GetDeviceState(string deviceId);
    
    // 统计
    IDataAcquisitionStatistics Statistics { get; }
}

public interface IDataAcquisitionStatistics
{
    long TotalReads { get; }
    long SuccessReads { get; }
    long FailedReads { get; }
    double ReadSuccessRate { get; }
    double AverageLatencyMs { get; }
}
```

### R-2: IAlarmService 报警服务

```csharp
public interface IAlarmService : IInitializable, IDisposable
{
    // 报警定义
    Task<OperateResult> RegisterAlarmAsync(AlarmDefinition definition, 
        CancellationToken ct = default);
    Task<OperateResult> UnregisterAlarmAsync(string alarmId, 
        CancellationToken ct = default);
    
    // 报警触发
    Task<OperateResult> RaiseAlarmAsync(Alarm alarm, CancellationToken ct = default);
    Task<OperateResult> AcknowledgeAlarmAsync(string alarmId, string userId,
        CancellationToken ct = default);
    Task<OperateResult> ClearAlarmAsync(string alarmId, string userId,
        CancellationToken ct = default);
    
    // 查询
    Task<IReadOnlyList<Alarm>> GetActiveAlarmsAsync(
        AlarmLevel? minLevel = null, CancellationToken ct = default);
    Task<IReadOnlyList<Alarm>> GetAlarmsAsync(DateTime startTime, DateTime endTime,
        CancellationToken ct = default);
    Task<Alarm?> GetAlarmAsync(string alarmId, CancellationToken ct = default);
    
    // 统计
    IAlarmStatistics Statistics { get; }
    
    // 事件
    event EventHandler<AlarmEventArgs>? AlarmRaised;
    event EventHandler<AlarmEventArgs>? AlarmAcknowledged;
    event EventHandler<AlarmEventArgs>? AlarmCleared;
}

public class AlarmDefinition
{
    public string Id { get; init; }
    public string Name { get; init; }
    public AlarmLevel Level { get; init; }
    public string Source { get; init; }
    public AlarmCondition Condition { get; init; }
    public string? MessageTemplate { get; init; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
}

public class Alarm
{
    public string Id { get; init; }
    public string DefinitionId { get; init; }
    public AlarmLevel Level { get; init; }
    public string Source { get; init; }
    public string Message { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
    public DateTime? ClearedAt { get; init; }
    public string? ClearedBy { get; init; }
    public object? Value { get; init; }
    public AlarmState State { get; init; }
}

public enum AlarmLevel { Info, Warning, Medium, High, Critical }
public enum AlarmState { Active, Acknowledged, Cleared }
public enum AlarmCondition { Equals, NotEquals, GreaterThan, LessThan, Between }
```

### R-3: IHistorianService 历史数据服务

```csharp
public interface IHistorianService : IInitializable, IDisposable
{
    // 写入历史数据
    Task<OperateResult> WriteAsync(HistorianDataPoint point, CancellationToken ct = default);
    Task<OperateResult> WriteBatchAsync(IEnumerable<HistorianDataPoint> points,
        CancellationToken ct = default);
    
    // 查询历史数据
    Task<IReadOnlyList<HistorianDataPoint>> QueryAsync(HistorianQuery query,
        CancellationToken ct = default);
    Task<IReadOnlyList<T>> QueryAsync<T>(HistorianQuery query,
        Func<HistorianDataPoint, T> selector, CancellationToken ct = default);
    
    // 聚合查询
    Task<IReadOnlyList<AggregatedDataPoint>> QueryAggregatedAsync(
        AggregatedQuery query, CancellationToken ct = default);
    
    // 存储管理
    Task<OperateResult> SetRetentionAsync(string tagName, TimeSpan retention,
        CancellationToken ct = default);
    Task<OperateResult<long>> GetDataSizeAsync(string tagName,
        CancellationToken ct = default);
    
    // 统计
    Task<HistorianStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

public class HistorianDataPoint
{
    public string TagName { get; init; }
    public object? Value { get; init; }
    public TagQuality Quality { get; init; }
    public DateTime Timestamp { get; init; }
}

public class HistorianQuery
{
    public string TagName { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int MaxPoints { get; init; } = 10000;
    public QueryMode Mode { get; init; } = QueryMode.Trend;
}

public enum QueryMode { Raw, Trend, Interpolated }

public class AggregatedQuery
{
    public string TagName { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public AggregationType AggregationType { get; init; }
    public TimeSpan Interval { get; init; }
}

public enum AggregationType { Average, Min, Max, Sum, Count, First, Last, StdDev }

public class AggregatedDataPoint
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public int Count { get; init; }
}
```

### R-4: IRecipeService 配方服务

```csharp
public interface IRecipeService : IInitializable
{
    // 配方管理
    Task<IReadOnlyList<Recipe>> GetAllRecipesAsync(string category,
        CancellationToken ct = default);
    Task<Recipe?> GetRecipeAsync(string recipeId, CancellationToken ct = default);
    Task<OperateResult> SaveRecipeAsync(Recipe recipe, CancellationToken ct = default);
    Task<OperateResult> DeleteRecipeAsync(string recipeId, CancellationToken ct = default);
    
    // 配方操作
    Task<OperateResult> LoadRecipeAsync(string recipeId, CancellationToken ct = default);
    Task<OperateResult> SaveCurrentAsRecipeAsync(string recipeId, string name,
        string? category = null, CancellationToken ct = default);
    Task<OperateResult> ValidateRecipeAsync(string recipeId, 
        CancellationToken ct = default);
}

public class Recipe
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public string? Author { get; init; }
    public IReadOnlyList<RecipeParameter> Parameters { get; init; }
}

public class RecipeParameter
{
    public string TagName { get; init; }
    public object? DefaultValue { get; init; }
    public object? MinValue { get; init; }
    public object? MaxValue { get; init; }
    public string? Unit { get; init; }
}
```

### R-5: IEventBus 事件总线

```csharp
public interface IEvent { }

public abstract class EventBase : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.Now;
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IEvent;
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IEvent;
    IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent;
    void Unsubscribe<TEvent>() where TEvent : IEvent;
}

public interface IEventHandler<TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
```

---

## Acceptance Criteria

- [ ] DataAcquisitionService 支持点位订阅
- [ ] DataAcquisitionService 支持批量读写
- [ ] DataAcquisitionService 提供统计信息
- [ ] AlarmService 支持报警定义和触发
- [ ] AlarmService 支持报警确认和清除
- [ ] AlarmService 支持分级和优先级
- [ ] HistorianService 支持高速写入
- [ ] HistorianService 支持聚合查询 (平均值、最大值、最小值等)
- [ ] HistorianService 支持数据保留策略
- [ ] RecipeService 支持配方 CRUD
- [ ] RecipeService 支持配方加载到设备
- [ ] EventBus 支持异步发布/订阅
- [ ] EventBus 支持取消订阅

---

## Dependencies

- `core-abstractions`
- `communication-layer`
- `data-access`
