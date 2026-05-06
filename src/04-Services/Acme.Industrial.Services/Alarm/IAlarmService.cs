namespace Acme.Industrial.Services.Alarm;

/// <summary>
/// 报警级别枚举
/// </summary>
public enum AlarmLevel
{
    /// <summary>信息</summary>
    Info = 0,

    /// <summary>警告</summary>
    Warning = 1,

    /// <summary>中等</summary>
    Medium = 2,

    /// <summary>高</summary>
    High = 3,

    /// <summary>严重</summary>
    Critical = 4
}

/// <summary>
/// 报警状态枚举
/// </summary>
public enum AlarmState
{
    /// <summary>激活</summary>
    Active = 0,

    /// <summary>已确认</summary>
    Acknowledged = 1,

    /// <summary>已清除</summary>
    Cleared = 2
}

/// <summary>
/// 报警条件枚举
/// </summary>
public enum AlarmCondition
{
    /// <summary>等于</summary>
    Equals = 0,

    /// <summary>不等于</summary>
    NotEquals = 1,

    /// <summary>大于</summary>
    GreaterThan = 2,

    /// <summary>小于</summary>
    LessThan = 3,

    /// <summary>介于两者之间</summary>
    Between = 4
}

/// <summary>
/// 报警定义
/// </summary>
public class AlarmDefinition
{
    /// <summary>报警ID</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>报警名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>报警级别</summary>
    public AlarmLevel Level { get; init; }

    /// <summary>报警来源（如设备ID）</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>触发条件</summary>
    public AlarmCondition Condition { get; init; }

    /// <summary>报警触发阈值</summary>
    public double Threshold { get; set; }

    /// <summary>上限阈值（Between条件用）</summary>
    public double? ThresholdMax { get; set; }

    /// <summary>消息模板</summary>
    public string? MessageTemplate { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>优先级（数值越大优先级越高）</summary>
    public int Priority { get; set; }
}

/// <summary>
/// 报警实例
/// </summary>
public class Alarm
{
    /// <summary>报警实例ID</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>关联的报警定义ID</summary>
    public string DefinitionId { get; init; } = string.Empty;

    /// <summary>报警级别</summary>
    public AlarmLevel Level { get; init; }

    /// <summary>报警来源</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>报警消息</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>发生时间</summary>
    public DateTime OccurredAt { get; init; } = DateTime.Now;

    /// <summary>确认时间</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>确认人</summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>清除时间</summary>
    public DateTime? ClearedAt { get; set; }

    /// <summary>清除人</summary>
    public string? ClearedBy { get; set; }

    /// <summary>触发时的值</summary>
    public object? Value { get; init; }

    /// <summary>当前状态</summary>
    public AlarmState State { get; set; } = AlarmState.Active;
}

/// <summary>
/// 报警事件参数
/// </summary>
public class AlarmEventArgs : EventArgs
{
    public Alarm Alarm { get; }

    public AlarmEventArgs(Alarm alarm) => Alarm = alarm;
}

/// <summary>
/// 报警统计接口
/// </summary>
public interface IAlarmStatistics
{
    /// <summary>当前激活的报警数</summary>
    int ActiveCount { get; }

    /// <summary>今日报警数</summary>
    int TodayCount { get; }

    /// <summary>未确认报警数</summary>
    int UnacknowledgedCount { get; }
}

/// <summary>
/// 报警服务接口
/// </summary>
public interface IAlarmService : Acme.Industrial.Core.Abstractions.IInitializable, IDisposable
{
    /// <summary>
    /// 注册报警定义
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> RegisterAlarmAsync(AlarmDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// 注销报警定义
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> UnregisterAlarmAsync(string alarmId, CancellationToken ct = default);

    /// <summary>
    /// 触发报警
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> RaiseAlarmAsync(Alarm alarm, CancellationToken ct = default);

    /// <summary>
    /// 确认报警
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> AcknowledgeAlarmAsync(string alarmId, string userId, CancellationToken ct = default);

    /// <summary>
    /// 清除报警
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> ClearAlarmAsync(string alarmId, string userId, CancellationToken ct = default);

    /// <summary>
    /// 获取当前激活的报警列表
    /// </summary>
    Task<IReadOnlyList<Alarm>> GetActiveAlarmsAsync(AlarmLevel? minLevel = null, CancellationToken ct = default);

    /// <summary>
    /// 获取指定时间范围内的报警记录
    /// </summary>
    Task<IReadOnlyList<Alarm>> GetAlarmsAsync(DateTime startTime, DateTime endTime, CancellationToken ct = default);

    /// <summary>
    /// 获取单个报警详情
    /// </summary>
    Task<Alarm?> GetAlarmAsync(string alarmId, CancellationToken ct = default);

    /// <summary>
    /// 检查条件是否触发报警
    /// </summary>
    void EvaluateCondition(string source, double value);

    /// <summary>
    /// 统计信息
    /// </summary>
    IAlarmStatistics Statistics { get; }

    /// <summary>报警触发事件</summary>
    event System.EventHandler<AlarmEventArgs>? AlarmRaised;

    /// <summary>报警确认事件</summary>
    event System.EventHandler<AlarmEventArgs>? AlarmAcknowledged;

    /// <summary>报警清除事件</summary>
    event System.EventHandler<AlarmEventArgs>? AlarmCleared;
}
