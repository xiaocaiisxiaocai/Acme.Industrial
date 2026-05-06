using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using System.Collections.Concurrent;

namespace Acme.Industrial.Services.Alarm;

/// <summary>
/// 报警服务实现
/// </summary>
public class AlarmService : IAlarmService
{
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, AlarmDefinition> _definitions = new();
    private readonly ConcurrentDictionary<string, Alarm> _activeAlarms = new();
    private readonly ConcurrentQueue<Alarm> _alarmHistory = new();
    private readonly AlarmStatistics _statistics = new();
    private bool _disposed;

    public AlarmService(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IAlarmStatistics Statistics => _statistics;

    public event EventHandler<AlarmEventArgs>? AlarmRaised;
    public event EventHandler<AlarmEventArgs>? AlarmAcknowledged;
    public event EventHandler<AlarmEventArgs>? AlarmCleared;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.Info("报警服务初始化完成，共加载 " + _definitions.Count + " 个报警定义");
        return Task.CompletedTask;
    }

    public Task<OperateResult> RegisterAlarmAsync(AlarmDefinition definition, CancellationToken ct = default)
    {
        if (definition == null)
            return Task.FromResult(OperateResult.Fail(-1, "报警定义为 null"));

        if (string.IsNullOrEmpty(definition.Id))
            return Task.FromResult(OperateResult.Fail(-1, "报警ID不能为空"));

        if (_definitions.TryAdd(definition.Id, definition))
        {
            _logger.Info("注册报警定义: " + definition.Id + " - " + definition.Name);
            return Task.FromResult(OperateResult.Ok());
        }

        return Task.FromResult(OperateResult.Fail(-1, "报警ID " + definition.Id + " 已存在"));
    }

    public Task<OperateResult> UnregisterAlarmAsync(string alarmId, CancellationToken ct = default)
    {
        if (_definitions.TryRemove(alarmId, out var removed))
        {
            _logger.Info("注销报警定义: " + alarmId);
            return Task.FromResult(OperateResult.Ok());
        }

        return Task.FromResult(OperateResult.Fail(-1, "报警ID " + alarmId + " 不存在"));
    }

    public Task<OperateResult> RaiseAlarmAsync(Alarm alarm, CancellationToken ct = default)
    {
        if (alarm == null)
            return Task.FromResult(OperateResult.Fail(-1, "报警对象为 null"));

        if (_activeAlarms.TryAdd(alarm.Id, alarm))
        {
            _alarmHistory.Enqueue(alarm);
            _statistics.RecordAlarm(alarm.Level);

            _logger.Warn("触发报警: " + alarm.Id + " [" + alarm.Level + "] " + alarm.Message);

            AlarmRaised?.Invoke(this, new AlarmEventArgs(alarm));
            return Task.FromResult(OperateResult.Ok());
        }

        return Task.FromResult(OperateResult.Fail(-1, "报警 " + alarm.Id + " 已存在"));
    }

    public Task<OperateResult> AcknowledgeAlarmAsync(string alarmId, string userId, CancellationToken ct = default)
    {
        if (!_activeAlarms.TryGetValue(alarmId, out var alarm))
            return Task.FromResult(OperateResult.Fail(-1, "报警 " + alarmId + " 不存在"));

        if (alarm.State == AlarmState.Cleared)
            return Task.FromResult(OperateResult.Fail(-1, "报警已清除，无法确认"));

        alarm.AcknowledgedAt = DateTime.Now;
        alarm.AcknowledgedBy = userId;
        alarm.State = AlarmState.Acknowledged;

        _statistics.RecordAcknowledge();

        _logger.Info("确认报警: " + alarmId + " by " + userId);
        AlarmAcknowledged?.Invoke(this, new AlarmEventArgs(alarm));

        return Task.FromResult(OperateResult.Ok());
    }

    public Task<OperateResult> ClearAlarmAsync(string alarmId, string userId, CancellationToken ct = default)
    {
        if (!_activeAlarms.TryGetValue(alarmId, out var alarm))
            return Task.FromResult(OperateResult.Fail(-1, "报警 " + alarmId + " 不存在"));

        alarm.ClearedAt = DateTime.Now;
        alarm.ClearedBy = userId;
        alarm.State = AlarmState.Cleared;

        if (_activeAlarms.TryRemove(alarmId, out var removed))
        {
            _statistics.RecordClear();
            _logger.Info("清除报警: " + alarmId + " by " + userId);
            AlarmCleared?.Invoke(this, new AlarmEventArgs(alarm));
        }

        return Task.FromResult(OperateResult.Ok());
    }

    public Task<IReadOnlyList<Alarm>> GetActiveAlarmsAsync(AlarmLevel? minLevel = null, CancellationToken ct = default)
    {
        var alarms = _activeAlarms.Values.AsEnumerable();

        if (minLevel.HasValue)
        {
            alarms = alarms.Where(a => a.Level >= minLevel.Value);
        }

        var result = alarms
            .OrderByDescending(a => a.Level)
            .ThenByDescending(a => a.OccurredAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Alarm>>(result);
    }

    public Task<IReadOnlyList<Alarm>> GetAlarmsAsync(DateTime startTime, DateTime endTime, CancellationToken ct = default)
    {
        var result = _alarmHistory
            .Where(a => a.OccurredAt >= startTime && a.OccurredAt <= endTime)
            .OrderByDescending(a => a.OccurredAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Alarm>>(result);
    }

    public Task<Alarm?> GetAlarmAsync(string alarmId, CancellationToken ct = default)
    {
        _activeAlarms.TryGetValue(alarmId, out var alarm);
        return Task.FromResult(alarm);
    }

    public void EvaluateCondition(string source, double value)
    {
        var matchingDefs = _definitions.Values
            .Where(d => d.Source == source && d.IsEnabled);

        foreach (var def in matchingDefs)
        {
            if (IsConditionMet(def, value))
            {
                if (!_activeAlarms.Values.Any(a => a.DefinitionId == def.Id && a.Source == source))
                {
                    var alarm = new Alarm
                    {
                        DefinitionId = def.Id,
                        Level = def.Level,
                        Source = source,
                        Message = string.Format(def.MessageTemplate ?? "{0} 触发报警", value),
                        Value = value
                    };

                    _ = RaiseAlarmAsync(alarm);
                }
            }
        }
    }

    private static bool IsConditionMet(AlarmDefinition def, double value)
    {
        return def.Condition switch
        {
            AlarmCondition.Equals => Math.Abs(value - def.Threshold) < 0.0001,
            AlarmCondition.NotEquals => Math.Abs(value - def.Threshold) >= 0.0001,
            AlarmCondition.GreaterThan => value > def.Threshold,
            AlarmCondition.LessThan => value < def.Threshold,
            AlarmCondition.Between => value >= def.Threshold && value <= (def.ThresholdMax ?? def.Threshold),
            _ => false
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _definitions.Clear();
        _activeAlarms.Clear();

        GC.SuppressFinalize(this);
    }

    private class AlarmStatistics : IAlarmStatistics
    {
        private int _activeCount;
        private int _todayCount;
        private int _unacknowledgedCount;
        private DateTime _lastResetDate = DateTime.Today;

        public int ActiveCount => _activeCount;
        public int TodayCount => _todayCount;
        public int UnacknowledgedCount => _unacknowledgedCount;

        public void RecordAlarm(AlarmLevel level)
        {
            Interlocked.Increment(ref _activeCount);
            Interlocked.Increment(ref _todayCount);
            Interlocked.Increment(ref _unacknowledgedCount);

            if (DateTime.Today > _lastResetDate)
            {
                _todayCount = 0;
                _lastResetDate = DateTime.Today;
            }
        }

        public void RecordAcknowledge()
        {
            Interlocked.Decrement(ref _unacknowledgedCount);
        }

        public void RecordClear()
        {
            Interlocked.Decrement(ref _activeCount);
        }
    }
}
