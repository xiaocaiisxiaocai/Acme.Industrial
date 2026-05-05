using System.Collections.Concurrent;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Infrastructure.Messaging;

/// <summary>
/// 消息处理结果。
/// </summary>
public class MessageHandleResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public object? Data { get; init; }

    public static MessageHandleResult Ok(object? data = null) => new()
    {
        Success = true,
        Data = data
    };

    public static MessageHandleResult Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// 消息处理器接口。
/// </summary>
public interface IMessageHandler
{
    Type MessageType { get; }
    Task<MessageHandleResult> HandleAsync(object message, CancellationToken ct);
}

/// <summary>
/// 通用消息处理器。
/// </summary>
public interface IMessageHandler<TMessage> : IMessageHandler
{
    Task<MessageHandleResult> HandleAsync(TMessage message, CancellationToken ct);
}

/// <summary>
/// 消息总线接口。
/// </summary>
public interface IMessageBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default) where TMessage : class;
    Task<Result> SendAsync<TMessage, Result>(TMessage message, CancellationToken ct = default)
        where TMessage : class;
    void RegisterHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : class;
    void UnregisterHandler<TMessage>() where TMessage : class;
}

/// <summary>
/// 内存消息总线实现。
/// </summary>
public class InMemoryMessageBus : IMessageBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, List<IMessageHandler>> _handlers = new();
    private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, Task>>> _asyncHandlers = new();
    private readonly IAppLogger _logger;
    private bool _disposed;

    public InMemoryMessageBus(IAppLogger logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class
    {
        var messageType = typeof(TMessage);

        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    var typedHandler = (IMessageHandler<TMessage>)handler;
                    await typedHandler.HandleAsync(message, ct);
                }
                catch (Exception ex)
                {
                    _logger.Error($"消息处理失败: {messageType.Name}", ex);
                }
            }
        }

        if (_asyncHandlers.TryGetValue(messageType, out var asyncHandlerList))
        {
            foreach (var handler in asyncHandlerList.ToList())
            {
                try
                {
                    await handler(message, ct);
                }
                catch (Exception ex)
                {
                    _logger.Error($"异步消息处理失败: {messageType.Name}", ex);
                }
            }
        }
    }

    public async Task<Result> SendAsync<TMessage, Result>(TMessage message, CancellationToken ct = default)
        where TMessage : class
    {
        var messageType = typeof(TMessage);

        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    var typedHandler = (IMessageHandler<TMessage>)handler;
                    var result = await typedHandler.HandleAsync(message, ct);
                    if (result.Success && result.Data is Result typedResult)
                    {
                        return typedResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"消息发送失败: {messageType.Name}", ex);
                }
            }
        }

        return default!;
    }

    public void RegisterHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : class
    {
        var messageType = typeof(TMessage);
        _handlers.AddOrUpdate(
            messageType,
            _ => new List<IMessageHandler> { handler },
            (_, list) =>
            {
                list.Add(handler);
                return list;
            });

        _logger.Debug($"消息处理器已注册: {messageType.Name}");
    }

    public void UnregisterHandler<TMessage>() where TMessage : class
    {
        var messageType = typeof(TMessage);
        _handlers.TryRemove(messageType, out _);
        _asyncHandlers.TryRemove(messageType, out _);
        _logger.Debug($"消息处理器已移除: {messageType.Name}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _handlers.Clear();
        _asyncHandlers.Clear();
    }
}

/// <summary>
/// 请求-响应消息接口。
/// </summary>
public interface IRequest { }

/// <summary>
/// 请求-响应消息处理器基类。
/// </summary>
public abstract class RequestHandler<TRequest, TResponse> : IMessageHandler<TRequest>
    where TRequest : class, IRequest
{
    public Type MessageType => typeof(TRequest);

    public Task<MessageHandleResult> HandleAsync(object message, CancellationToken ct)
    {
        return HandleAsync((TRequest)message, ct);
    }

    public abstract Task<MessageHandleResult> HandleAsync(TRequest request, CancellationToken ct);
}

/// <summary>
/// 命令消息（用于触发操作）。
/// </summary>
public interface ICommand : IRequest { }

/// <summary>
/// 查询消息（用于请求数据）。
/// </summary>
public interface IQuery<TResult> : IRequest { }

/// <summary>
/// 命令处理器基类。
/// </summary>
public abstract class CommandHandler<TCommand> : RequestHandler<TCommand, Unit>
    where TCommand : class, ICommand
{
    public override Task<MessageHandleResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        return ExecuteAsync(command, ct);
    }

    protected abstract Task<MessageHandleResult> ExecuteAsync(TCommand command, CancellationToken ct);
}

/// <summary>
/// 查询处理器基类。
/// </summary>
public abstract class QueryHandler<TQuery, TResult> : RequestHandler<TQuery, TResult>
    where TQuery : class, IQuery<TResult>
{
    public override Task<MessageHandleResult> HandleAsync(TQuery query, CancellationToken ct)
    {
        return ExecuteAsync(query, ct);
    }

    protected abstract Task<MessageHandleResult> ExecuteAsync(TQuery query, CancellationToken ct);
}

/// <summary>
/// 空结果类型。
/// </summary>
public class Unit
{
    public static Unit Instance { get; } = new();
}
