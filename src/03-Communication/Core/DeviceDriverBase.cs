using System.Diagnostics;
using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Core;

/// <summary>
/// 设备驱动基类 - 封装连接状态管理、重试、统计、心跳、断线重连。
/// </summary>
public abstract class DeviceDriverBase : IDeviceDriver
{
    private readonly SemaphoreSlim _connLock = new(1, 1);
    private CancellationTokenSource? _heartbeatCts;
    protected readonly IAppLogger Logger;
    protected readonly IByteTransform ByteTransform;

    public string DeviceId => Options.DeviceId;
    public string Protocol => Options.Protocol;
    public ConnectionOptions Options { get; }
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public CommunicationStatistics Statistics { get; } = new();

    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;
    public event EventHandler<CommunicationErrorEventArgs>? ErrorOccurred;

    protected DeviceDriverBase(ConnectionOptions options, IAppLoggerFactory loggerFactory,
        IByteTransform byteTransform)
    {
        Options = options;
        Logger = loggerFactory.CreateLogger($"Driver.{options.Protocol}.{options.DeviceId}");
        ByteTransform = byteTransform;
    }

    // 模板方法（子类实现）
    protected abstract Task<OperateResult> ConnectCoreAsync(CancellationToken ct);
    protected abstract Task<OperateResult> DisconnectCoreAsync(CancellationToken ct);
    protected abstract Task<OperateResult<byte[]>> ReadRawCoreAsync(
        string address, ushort length, CancellationToken ct);
    protected abstract Task<OperateResult> WriteRawCoreAsync(
        string address, byte[] data, CancellationToken ct);
    protected abstract Task<OperateResult> PingCoreAsync(CancellationToken ct);
    protected abstract (ushort byteLength, ushort registerCount) GetByteLength(Tag tag);
    protected abstract object ParseValue(Tag tag, byte[] bytes);
    protected abstract byte[] SerializeValue(Tag tag, object value);
    protected abstract DataType ResolveDataType<T>();

    // 公共实现
    public virtual async Task<OperateResult> ConnectAsync(CancellationToken ct = default)
    {
        await _connLock.WaitAsync(ct);
        try
        {
            if (State == ConnectionState.Connected) return OperateResult.Ok();

            ChangeState(ConnectionState.Connecting);
            var result = await WithRetry(() => ConnectCoreAsync(ct), Options.RetryCount, ct);

            if (result.IsSuccess)
            {
                ChangeState(ConnectionState.Connected);
                Statistics.ConnectedSince = DateTime.Now;
                StartHeartbeat();
            }
            else
            {
                ChangeState(ConnectionState.Faulted);
            }
            return result;
        }
        finally { _connLock.Release(); }
    }

    public virtual async Task<OperateResult> DisconnectAsync(CancellationToken ct = default)
    {
        StopHeartbeat();
        await _connLock.WaitAsync(ct);
        try
        {
            var result = await DisconnectCoreAsync(ct);
            ChangeState(ConnectionState.Disconnected);
            return result;
        }
        finally { _connLock.Release(); }
    }

    public virtual Task<OperateResult> PingAsync(CancellationToken ct = default) =>
        WithRetry(() => PingCoreAsync(ct), 1, ct);

    public virtual async Task<OperateResult<byte[]>> ReadRawAsync(
        string address, ushort length, CancellationToken ct = default)
    {
        if (State != ConnectionState.Connected)
            return OperateResult.Fail<byte[]>(
                ErrorCode.CommNotConnected, "Device not connected.");

        var sw = Stopwatch.StartNew();
        var result = await WithRetry(() => ReadRawCoreAsync(address, length, ct),
            Options.RetryCount, ct);
        sw.Stop();

        Statistics.TotalReads++;
        if (result.IsSuccess)
        {
            Statistics.SuccessReads++;
            Statistics.TotalBytesReceived += result.Content?.Length ?? 0;
            Statistics.LastSuccessTime = DateTime.Now;
            UpdateAvgLatency(sw.ElapsedMilliseconds);
        }
        else
        {
            Statistics.FailedReads++;
            Statistics.LastErrorTime = DateTime.Now;
            Statistics.LastErrorMessage = result.Message;
            OnError(result);
        }
        result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
        return result;
    }

    public virtual async Task<OperateResult> WriteRawAsync(
        string address, byte[] data, CancellationToken ct = default)
    {
        if (State != ConnectionState.Connected)
            return OperateResult.Fail(ErrorCode.CommNotConnected, "Device not connected.");

        var sw = Stopwatch.StartNew();
        var result = await WithRetry(() => WriteRawCoreAsync(address, data, ct),
            Options.RetryCount, ct);
        sw.Stop();

        Statistics.TotalWrites++;
        if (result.IsSuccess)
        {
            Statistics.SuccessWrites++;
            Statistics.TotalBytesSent += data.Length;
            Statistics.LastSuccessTime = DateTime.Now;
        }
        else
        {
            Statistics.FailedWrites++;
            OnError(result);
        }
        result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
        return result;
    }

    public virtual async Task<OperateResult<TagValue>> ReadAsync(
        Tag tag, CancellationToken ct = default)
    {
        var (length, _) = GetByteLength(tag);
        var raw = await ReadRawAsync(tag.Address, length, ct);
        if (!raw.IsSuccess) return OperateResult.Fail<TagValue>(raw);

        var value = ParseValue(tag, raw.Content!);
        return OperateResult.Ok(new TagValue
        {
            TagName = tag.Name,
            Value = value,
            Quality = TagQuality.Good,
            Timestamp = DateTime.Now,
            RawBytes = raw.Content
        });
    }

    public virtual async Task<OperateResult<T>> ReadAsync<T>(
        string address, CancellationToken ct = default)
    {
        var tag = new Tag { Address = address, DataType = ResolveDataType<T>() };
        var r = await ReadAsync(tag, ct);
        if (!r.IsSuccess) return OperateResult.Fail<T>(r);
        return OperateResult.Ok((T)r.Content!.Value!);
    }

    public virtual async Task<OperateResult> WriteAsync(
        Tag tag, object value, CancellationToken ct = default)
    {
        var bytes = SerializeValue(tag, value);
        return await WriteRawAsync(tag.Address, bytes, ct);
    }

    public virtual Task<OperateResult> WriteAsync<T>(
        string address, T value, CancellationToken ct = default)
    {
        var tag = new Tag { Address = address, DataType = ResolveDataType<T>() };
        return WriteAsync(tag, value!, ct);
    }

    public virtual async Task<OperateResult<IReadOnlyList<TagValue>>> ReadBatchAsync(
        IEnumerable<Tag> tags, CancellationToken ct = default)
    {
        var list = new List<TagValue>();
        foreach (var tag in tags)
        {
            ct.ThrowIfCancellationRequested();
            var r = await ReadAsync(tag, ct);
            list.Add(r.IsSuccess
                ? r.Content!
                : new TagValue { TagName = tag.Name, Quality = TagQuality.Bad });
        }
        return OperateResult.Ok<IReadOnlyList<TagValue>>(list);
    }

    public virtual async Task<OperateResult> WriteBatchAsync(
        IEnumerable<KeyValuePair<Tag, object>> writes, CancellationToken ct = default)
    {
        foreach (var kv in writes)
        {
            ct.ThrowIfCancellationRequested();
            var r = await WriteAsync(kv.Key, kv.Value, ct);
            if (!r.IsSuccess) return r;
        }
        return OperateResult.Ok();
    }

    // 工具方法
    protected void ChangeState(ConnectionState newState)
    {
        if (State == newState) return;
        var old = State;
        State = newState;
        Logger.Info($"Device [{DeviceId}] state: {old} -> {newState}");
        StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(old, newState));
    }

    private void OnError(OperateResult result) =>
        ErrorOccurred?.Invoke(this,
            new CommunicationErrorEventArgs(result.ErrorCode, result.Message, result.Exception));

    private async Task<OperateResult<T>> WithRetry<T>(
        Func<Task<OperateResult<T>>> action, int retry, CancellationToken ct)
    {
        OperateResult<T> last = null!;
        for (var i = 0; i <= retry; i++)
        {
            ct.ThrowIfCancellationRequested();
            last = await action();
            if (last.IsSuccess) return last;
            if (i < retry) await Task.Delay(Options.RetryIntervalMs, ct);
        }
        return last;
    }

    private async Task<OperateResult> WithRetry(
        Func<Task<OperateResult>> action, int retry, CancellationToken ct)
    {
        OperateResult last = null!;
        for (var i = 0; i <= retry; i++)
        {
            ct.ThrowIfCancellationRequested();
            last = await action();
            if (last.IsSuccess) return last;
            if (i < retry) await Task.Delay(Options.RetryIntervalMs, ct);
        }
        return last;
    }

    private void StartHeartbeat()
    {
        StopHeartbeat();
        if (Options.HeartbeatIntervalMs <= 0) return;
        _heartbeatCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            var token = _heartbeatCts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Options.HeartbeatIntervalMs, token);
                    var r = await PingAsync(token);
                    if (!r.IsSuccess && State == ConnectionState.Connected)
                    {
                        Logger.Warn($"[{DeviceId}] heartbeat failed, reconnecting...");
                        ChangeState(ConnectionState.Reconnecting);
                        await ConnectAsync(token);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Logger.Warn($"[{DeviceId}] heartbeat error", ex); }
            }
        });
    }

    private void StopHeartbeat()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
    }

    private void UpdateAvgLatency(long ms)
    {
        Statistics.AverageReadLatencyMs =
            (Statistics.AverageReadLatencyMs * (Statistics.SuccessReads - 1) + ms)
            / Math.Max(1, Statistics.SuccessReads);
    }

    public virtual async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
