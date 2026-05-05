using System.Collections.Concurrent;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Infrastructure.Scheduling;

/// <summary>
/// 任务状态。
/// </summary>
public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// 任务信息。
/// </summary>
public class ScheduledTaskInfo
{
    public string TaskId { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskStatus Status { get; set; }
    public DateTime? NextRunTime { get; set; }
    public DateTime? LastRunTime { get; set; }
    public DateTime? LastCompletedTime { get; set; }
    public Exception? LastError { get; set; }
    public int RunCount { get; set; }
}

/// <summary>
/// 触发器类型。
/// </summary>
public enum TriggerType
{
    Interval,
    Cron,
    Daily,
    OneTime
}

/// <summary>
/// 任务触发器接口。
/// </summary>
public interface ITaskTrigger
{
    TriggerType Type { get; }
    DateTime? GetNextOccurrence(DateTime fromTime);
    bool ShouldRun(DateTime currentTime);
}

/// <summary>
/// 间隔触发器。
/// </summary>
public class IntervalTrigger : ITaskTrigger
{
    public TimeSpan Interval { get; }

    public TriggerType Type => TriggerType.Interval;

    public IntervalTrigger(TimeSpan interval)
    {
        Interval = interval;
    }

    public DateTime? GetNextOccurrence(DateTime fromTime)
    {
        return fromTime.Add(Interval);
    }

    public bool ShouldRun(DateTime currentTime)
    {
        return true;
    }
}

/// <summary>
/// 每日触发器。
/// </summary>
public class DailyTrigger : ITaskTrigger
{
    public TimeSpan TimeOfDay { get; }

    public TriggerType Type => TriggerType.Daily;

    public DailyTrigger(TimeSpan timeOfDay)
    {
        TimeOfDay = timeOfDay;
    }

    public DateTime? GetNextOccurrence(DateTime fromTime)
    {
        var today = fromTime.Date.Add(TimeOfDay);
        return today > fromTime ? today : today.AddDays(1);
    }

    public bool ShouldRun(DateTime currentTime)
    {
        return currentTime.TimeOfDay >= TimeOfDay && currentTime.TimeOfDay < TimeOfDay.Add(TimeSpan.FromSeconds(1));
    }
}

/// <summary>
/// 一次性触发器。
/// </summary>
public class OneTimeTrigger : ITaskTrigger
{
    public DateTime RunTime { get; }

    public TriggerType Type => TriggerType.OneTime;

    public OneTimeTrigger(DateTime runTime)
    {
        RunTime = runTime;
    }

    public DateTime? GetNextOccurrence(DateTime fromTime)
    {
        return RunTime > fromTime ? RunTime : null;
    }

    public bool ShouldRun(DateTime currentTime)
    {
        return currentTime >= RunTime && currentTime < RunTime.AddSeconds(1);
    }
}

/// <summary>
/// 任务调度器实现。
/// </summary>
public class TaskScheduler : IDisposable
{
    private readonly ConcurrentDictionary<string, ScheduledTask> _tasks = new();
    private readonly ConcurrentDictionary<string, ScheduledTaskInfo> _taskInfos = new();
    private readonly IAppLogger _logger;
    private readonly System.Threading.Timer _timer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public event EventHandler<ScheduledTaskInfo>? TaskCompleted;
    public event EventHandler<ScheduledTaskInfo>? TaskFailed;
    public event EventHandler<ScheduledTaskInfo>? TaskStarted;

    public TaskScheduler(IAppLogger logger)
    {
        _logger = logger;
        _timer = new System.Threading.Timer(ExecutePendingTasks, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public string AddTask(
        string name,
        Func<CancellationToken, Task> action,
        ITaskTrigger trigger,
        string? description = null)
    {
        var task = new ScheduledTask
        {
            TaskId = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Action = action,
            Trigger = trigger,
            NextRunTime = trigger.GetNextOccurrence(DateTime.Now)
        };

        _tasks[task.TaskId] = task;
        _taskInfos[task.TaskId] = new ScheduledTaskInfo
        {
            TaskId = task.TaskId,
            Name = task.Name,
            Description = task.Description,
            Status = TaskStatus.Pending,
            NextRunTime = task.NextRunTime
        };

        _logger.Info($"任务已添加: {name}, TaskId={task.TaskId}");
        return task.TaskId;
    }

    public void RemoveTask(string taskId)
    {
        if (_tasks.TryRemove(taskId, out var task))
        {
            _logger.Info($"任务已移除: {task.Name}, TaskId={taskId}");
        }
    }

    public ScheduledTaskInfo? GetTaskInfo(string taskId)
    {
        return _taskInfos.TryGetValue(taskId, out var info) ? info : null;
    }

    public IReadOnlyList<ScheduledTaskInfo> GetAllTasks()
    {
        return _taskInfos.Values.ToList();
    }

    public async Task ExecuteNowAsync(string taskId, CancellationToken ct = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"任务不存在: {taskId}");
        }

        await ExecuteTaskAsync(task, ct);
    }

    private async void ExecutePendingTasks(object? state)
    {
        if (_disposed) return;

        var now = DateTime.Now;

        foreach (var (taskId, task) in _tasks)
        {
            if (task.IsRunning || !task.Trigger.ShouldRun(now))
            {
                continue;
            }

            task.NextRunTime = task.Trigger.GetNextOccurrence(now);

            if (_taskInfos.TryGetValue(taskId, out var info))
            {
                info.NextRunTime = task.NextRunTime;
            }

            _ = Task.Run(() => ExecuteTaskAsync(task, CancellationToken.None));
        }
    }

    private async Task ExecuteTaskAsync(ScheduledTask task, CancellationToken ct)
    {
        if (_tasks.TryGetValue(task.TaskId, out var currentTask))
        {
            if (currentTask.IsRunning) return;
            currentTask.IsRunning = true;
        }

        var info = _taskInfos.GetValueOrDefault(task.TaskId);
        info!.Status = TaskStatus.Running;
        info.LastRunTime = DateTime.Now;
        info.RunCount++;

        TaskStarted?.Invoke(this, info);
        _logger.Debug($"任务开始执行: {task.Name}");

        try
        {
            using var timeoutCts = new CancellationTokenSource(task.Timeout ?? TimeSpan.FromMinutes(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            await task.Action(linkedCts.Token);

            info.Status = TaskStatus.Completed;
            info.LastCompletedTime = DateTime.Now;
            info.LastError = null;

            TaskCompleted?.Invoke(this, info);
            _logger.Debug($"任务执行完成: {task.Name}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            info.Status = TaskStatus.Cancelled;
            _logger.Warn($"任务已取消: {task.Name}");
        }
        catch (OperationCanceledException)
        {
            info.Status = TaskStatus.Failed;
            info.LastError = new TimeoutException("任务执行超时");
            TaskFailed?.Invoke(this, info);
            _logger.Error($"任务执行超时: {task.Name}", info.LastError);
        }
        catch (Exception ex)
        {
            info.Status = TaskStatus.Failed;
            info.LastError = ex;
            TaskFailed?.Invoke(this, info);
            _logger.Error($"任务执行失败: {task.Name}", ex);
        }
        finally
        {
            if (_tasks.TryGetValue(task.TaskId, out var runningTask))
            {
                runningTask.IsRunning = false;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
        _semaphore.Dispose();
        _tasks.Clear();
    }

    private class ScheduledTask
    {
        public string TaskId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Func<CancellationToken, Task> Action { get; init; } = null!;
        public ITaskTrigger Trigger { get; init; } = null!;
        public DateTime? NextRunTime { get; set; }
        public TimeSpan? Timeout { get; set; }
        public bool IsRunning { get; set; }
    }
}

/// <summary>
/// 后台任务服务（用于不需要严格调度的后台任务）。
/// </summary>
public class BackgroundTaskService : IDisposable
{
    private readonly ConcurrentDictionary<string, BackgroundTask> _tasks = new();
    private readonly IAppLogger _logger;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public BackgroundTaskService(IAppLogger logger)
    {
        _logger = logger;
        _cts = new CancellationTokenSource();
    }

    public string Start(string name, Func<CancellationToken, Task> work, bool longRunning = false)
    {
        var taskId = Guid.NewGuid().ToString();
        var task = new BackgroundTask
        {
            TaskId = taskId,
            Name = name,
            Work = work,
            CancellationToken = _cts.Token,
            IsLongRunning = longRunning
        };

        task.Task = longRunning
            ? Task.Factory.StartNew(() => work(_cts.Token), _cts.Token, TaskCreationOptions.LongRunning, System.Threading.Tasks.TaskScheduler.Default)
            : Task.Run(() => work(_cts.Token), _cts.Token);

        _tasks[taskId] = task;
        _logger.Info($"后台任务已启动: {name}, TaskId={taskId}");

        return taskId;
    }

    public void Stop(string taskId)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            _logger.Info($"后台任务已停止: {task.Name}, TaskId={taskId}");
        }
    }

    public async Task StopAllAsync(TimeSpan? timeout = null)
    {
        var waitTime = timeout ?? TimeSpan.FromSeconds(30);
        var deadline = DateTime.Now.Add(waitTime);

        _cts.Cancel();

        foreach (var task in _tasks.Values.Where(t => t.Task != null))
        {
            var remaining = deadline - DateTime.Now;
            if (remaining > TimeSpan.Zero)
            {
                try
                {
                    await Task.WhenAny(task.Task!, Task.Delay(remaining));
                }
                catch { }
            }
        }

        _tasks.Clear();
    }

    public bool IsRunning(string taskId)
    {
        return _tasks.TryGetValue(taskId, out var task)
               && task.Task is { IsCompleted: false };
    }

    public IReadOnlyList<(string TaskId, string Name, bool IsRunning)> GetAllTasks()
    {
        return _tasks.Select(t => (t.Key, t.Value.Name, t.Value.Task?.IsCompleted == false))
                     .ToList();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _tasks.Clear();
    }

    private class BackgroundTask
    {
        public string TaskId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public Func<CancellationToken, Task> Work { get; init; } = null!;
        public CancellationToken CancellationToken { get; init; }
        public Task? Task { get; set; }
        public bool IsLongRunning { get; init; }
    }
}
