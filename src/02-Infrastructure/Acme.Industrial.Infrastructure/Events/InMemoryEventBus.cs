using System.Collections.Concurrent;
using Acme.Industrial.Core.Events;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Abstractions;

namespace Acme.Industrial.Infrastructure.Events;

/// <summary>
/// 内存事件总线实现。
/// </summary>
public class InMemoryEventBus : IEventBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, List<WeakReference>> _handlers = new();
    private readonly ConcurrentDictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _asyncHandlers = new();
    private readonly IAppLogger _logger;
    private readonly IServiceResolver _resolver;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public InMemoryEventBus(IAppLogger logger, IServiceResolver resolver)
    {
        _logger = logger;
        _resolver = resolver;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IEvent
    {
        if (_disposed) return;

        var eventType = typeof(TEvent);
        _logger.Debug($"发布事件: {eventType.Name}");

        await _semaphore.WaitAsync(ct);
        try
        {
            var handlers = GetHandlers<TEvent>();
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(@event, ct);
                }
                catch (Exception ex)
                {
                    _logger.Error($"事件处理器执行失败: {eventType.Name}", ex);
                }
            }

            if (_asyncHandlers.TryGetValue(eventType, out var asyncHandlerList))
            {
                foreach (var handler in asyncHandlerList.ToList())
                {
                    try
                    {
                        await handler(@event, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"异步事件处理器执行失败: {eventType.Name}", ex);
                    }
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        var wrapper = new Func<IEvent, CancellationToken, Task>((e, ct) => handler((TEvent)e, ct));

        _asyncHandlers.AddOrUpdate(
            eventType,
            _ => new List<Func<IEvent, CancellationToken, Task>> { wrapper },
            (_, list) =>
            {
                list.Add(wrapper);
                return list;
            });

        _logger.Debug($"订阅事件: {eventType.Name}");
        return new EventSubscription(() => UnsubscribeHandler(eventType, wrapper));
    }

    public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);

        _handlers.AddOrUpdate(
            eventType,
            _ => new List<WeakReference> { new(handler) },
            (_, list) =>
            {
                list.Add(new WeakReference(handler));
                return list;
            });

        _logger.Debug($"订阅事件处理器: {eventType.Name}");
        return new EventSubscription(() => UnsubscribeHandler(eventType, handler));
    }

    public void Unsubscribe<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        _handlers.TryRemove(eventType, out _);
        _asyncHandlers.TryRemove(eventType, out _);
        _logger.Debug($"取消订阅事件: {eventType.Name}");
    }

    private List<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        var result = new List<IEventHandler<TEvent>>();

        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var weakRef in handlers.ToList())
            {
                if (weakRef.Target is IEventHandler<TEvent> handler)
                {
                    result.Add(handler);
                }
            }
        }

        return result;
    }

    private void UnsubscribeHandler(Type eventType, object handler)
    {
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            var toRemove = handlers.Where(w => w.Target == handler).ToList();
            foreach (var item in toRemove)
            {
                handlers.Remove(item);
            }
        }

        if (_asyncHandlers.TryGetValue(eventType, out var asyncHandlers))
        {
            asyncHandlers.RemoveAll(h => h == handler);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
        _handlers.Clear();
        _asyncHandlers.Clear();
    }

    private class EventSubscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public EventSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _unsubscribe();
        }
    }
}

/// <summary>
/// 聚合事件处理器基类（用于处理多个相关事件）。
/// </summary>
public abstract class AggregateEventHandler :
    IEventHandler<DeviceConnectedEvent>,
    IEventHandler<DeviceDisconnectedEvent>,
    IEventHandler<DeviceDataChangedEvent>
{
    public abstract Task HandleAsync(DeviceConnectedEvent @event, CancellationToken ct);
    public abstract Task HandleAsync(DeviceDisconnectedEvent @event, CancellationToken ct);
    public abstract Task HandleAsync(DeviceDataChangedEvent @event, CancellationToken ct);
}

/// <summary>
/// 设备连接事件。
/// </summary>
public class DeviceConnectedEvent : EventBase
{
    public string DeviceId { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
}

/// <summary>
/// 设备断开连接事件。
/// </summary>
public class DeviceDisconnectedEvent : EventBase
{
    public string DeviceId { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

/// <summary>
/// 设备数据变化事件。
/// </summary>
public class DeviceDataChangedEvent : EventBase
{
    public string DeviceId { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// 报警事件。
/// </summary>
public class AlarmRaisedEvent : EventBase
{
    public string AlarmId { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public string TagName { get; init; } = string.Empty;
    public AlarmLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? TriggerValue { get; init; }
    public DateTime Timestamp { get; init; }
}

public enum AlarmLevel
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// 报警确认事件。
/// </summary>
public class AlarmAcknowledgedEvent : EventBase
{
    public string AlarmId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public DateTime AcknowledgedAt { get; init; }
}

/// <summary>
/// 事件发布器扩展方法。
/// </summary>
public static class EventBusExtensions
{
    public static async Task PublishDeviceConnectedAsync(
        this IEventBus bus,
        string deviceId,
        string deviceName,
        string connectionString)
    {
        await bus.PublishAsync(new DeviceConnectedEvent
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            ConnectionString = connectionString
        });
    }

    public static async Task PublishDeviceDisconnectedAsync(
        this IEventBus bus,
        string deviceId,
        string deviceName,
        string? reason = null)
    {
        await bus.PublishAsync(new DeviceDisconnectedEvent
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            Reason = reason
        });
    }

    public static async Task PublishDataChangedAsync(
        this IEventBus bus,
        string deviceId,
        string tagName,
        object? oldValue,
        object? newValue)
    {
        await bus.PublishAsync(new DeviceDataChangedEvent
        {
            DeviceId = deviceId,
            TagName = tagName,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.Now
        });
    }

    public static async Task PublishAlarmAsync(
        this IEventBus bus,
        string alarmId,
        string deviceId,
        string tagName,
        AlarmLevel level,
        string message,
        object? triggerValue)
    {
        await bus.PublishAsync(new AlarmRaisedEvent
        {
            AlarmId = alarmId,
            DeviceId = deviceId,
            TagName = tagName,
            Level = level,
            Message = message,
            TriggerValue = triggerValue,
            Timestamp = DateTime.Now
        });
    }
}
