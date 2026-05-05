using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.Results;
using Acme.Industrial.Communication.Abstractions.Models;
using Acme.Industrial.Communication.Abstractions.Enumerations;

namespace Acme.Industrial.Communication.Abstractions;

/// <summary>
/// 设备驱动接口 - 所有具体协议驱动实现此接口。
/// </summary>
public interface IDeviceDriver : System.IAsyncDisposable
{
    /// <summary>设备 ID。</summary>
    string DeviceId { get; }

    /// <summary>协议类型。</summary>
    string Protocol { get; }

    /// <summary>连接状态。</summary>
    ConnectionState State { get; }

    /// <summary>连接选项。</summary>
    ConnectionOptions Options { get; }

    /// <summary>统计信息。</summary>
    CommunicationStatistics Statistics { get; }

    /// <summary>
    /// 连接状态变更事件。
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 错误事件。
    /// </summary>
    event EventHandler<CommunicationErrorEventArgs>? ErrorOccurred;

    // 连接管理
    /// <summary>
    /// 连接。
    /// </summary>
    Task<OperateResult> ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 断开连接。
    /// </summary>
    Task<OperateResult> DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Ping。
    /// </summary>
    Task<OperateResult> PingAsync(CancellationToken ct = default);

    // 单点读
    /// <summary>
    /// 读取点位。
    /// </summary>
    Task<OperateResult<TagValue>> ReadAsync(Tag tag, CancellationToken ct = default);

    /// <summary>
    /// 读取值。
    /// </summary>
    Task<OperateResult<T>> ReadAsync<T>(string address, CancellationToken ct = default);

    // 单点写
    /// <summary>
    /// 写入点位。
    /// </summary>
    Task<OperateResult> WriteAsync(Tag tag, object value, CancellationToken ct = default);

    /// <summary>
    /// 写入值。
    /// </summary>
    Task<OperateResult> WriteAsync<T>(string address, T value, CancellationToken ct = default);

    // 批量读写
    /// <summary>
    /// 批量读取。
    /// </summary>
    Task<OperateResult<IReadOnlyList<TagValue>>> ReadBatchAsync(
        IEnumerable<Tag> tags, CancellationToken ct = default);

    /// <summary>
    /// 批量写入。
    /// </summary>
    Task<OperateResult> WriteBatchAsync(
        IEnumerable<KeyValuePair<Tag, object>> writes, CancellationToken ct = default);

    // 原始字节读写
    /// <summary>
    /// 原始读取。
    /// </summary>
    Task<OperateResult<byte[]>> ReadRawAsync(string address, ushort length,
        CancellationToken ct = default);

    /// <summary>
    /// 原始写入。
    /// </summary>
    Task<OperateResult> WriteRawAsync(string address, byte[] data,
        CancellationToken ct = default);
}
