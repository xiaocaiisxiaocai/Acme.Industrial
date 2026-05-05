namespace Acme.Industrial.Core.Events;

/// <summary>
/// 事件接口。
/// </summary>
public interface IEvent { }

/// <summary>
/// 事件基类。
/// </summary>
public abstract class EventBase : IEvent
{
    /// <summary>事件 ID。</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>时间戳。</summary>
    public DateTime Timestamp { get; } = DateTime.Now;
}

/// <summary>
/// 事件处理器接口。
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// 处理事件。
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

/// <summary>
/// 事件总线接口。
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件（异步 Fire-and-forget）。
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent;

    /// <summary>
    /// 订阅事件。
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent;

    /// <summary>
    /// 订阅事件（使用处理器）。
    /// </summary>
    IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;

    /// <summary>
    /// 取消订阅。
    /// </summary>
    void Unsubscribe<TEvent>() where TEvent : IEvent;
}
