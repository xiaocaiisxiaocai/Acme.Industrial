using System.Collections.Generic;

namespace Acme.Industrial.Services.DataAcquisition;

/// <summary>
/// 数据采集服务接口
/// 提供点位订阅、批量读写、设备管理等功能
/// </summary>
public interface IDataAcquisitionService : Acme.Industrial.Core.Abstractions.IInitializable, IDisposable
{
    /// <summary>
    /// 订阅点位变化
    /// </summary>
    IDisposable Subscribe(Acme.Industrial.Communication.Abstractions.Models.Tag[] tags, System.Action<Acme.Industrial.Communication.Abstractions.Models.TagValue> callback);

    /// <summary>
    /// 按设备ID和地址订阅
    /// </summary>
    IDisposable Subscribe(string deviceId, string[] addresses, System.Action<Acme.Industrial.Communication.Abstractions.Models.TagValue> callback);

    /// <summary>
    /// 批量读取点位值
    /// </summary>
    Task<System.Collections.Generic.Dictionary<string, Acme.Industrial.Communication.Abstractions.Models.TagValue>> ReadBatchAsync(
        string deviceId, string[] addresses, CancellationToken ct = default);

    /// <summary>
    /// 读取单个点位
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult<Acme.Industrial.Communication.Abstractions.Models.TagValue>> ReadAsync(
        string deviceId, string address, CancellationToken ct = default);

    /// <summary>
    /// 写入点位值
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> WriteAsync(string deviceId, string address, object value, CancellationToken ct = default);

    /// <summary>
    /// 连接设备
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> ConnectDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// 断开设备连接
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> DisconnectDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// 获取设备状态
    /// </summary>
    Acme.Industrial.Communication.Abstractions.Enumerations.ConnectionState GetDeviceState(string deviceId);

    /// <summary>
    /// 统计信息
    /// </summary>
    IDataAcquisitionStatistics Statistics { get; }
}

/// <summary>
/// 数据采集统计接口
/// </summary>
public interface IDataAcquisitionStatistics
{
    /// <summary>总读取次数</summary>
    long TotalReads { get; }

    /// <summary>成功读取次数</summary>
    long SuccessReads { get; }

    /// <summary>失败读取次数</summary>
    long FailedReads { get; }

    /// <summary>读取成功率</summary>
    double ReadSuccessRate { get; }

    /// <summary>平均延迟（毫秒）</summary>
    double AverageLatencyMs { get; }
}
