using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.Results;
using Acme.Industrial.Communication.Abstractions.Models;
using Acme.Industrial.Communication.Abstractions.Enumerations;

namespace Acme.Industrial.Communication.Abstractions;

/// <summary>
/// 设备管理器接口 - 统一管理多个设备驱动。
/// </summary>
public interface IDeviceManager : System.IAsyncDisposable
{
    /// <summary>
    /// 添加设备。
    /// </summary>
    /// <param name="options">连接选项。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>操作结果。</returns>
    Task<OperateResult> AddDeviceAsync(ConnectionOptions options, CancellationToken ct = default);

    /// <summary>
    /// 移除设备。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>操作结果。</returns>
    Task<OperateResult> RemoveDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// 获取设备驱动。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <returns>设备驱动实例，不存在则返回 null。</returns>
    IDeviceDriver? GetDevice(string deviceId);

    /// <summary>
    /// 获取所有设备驱动。
    /// </summary>
    /// <returns>设备驱动列表。</returns>
    IReadOnlyList<IDeviceDriver> GetAllDevices();

    /// <summary>
    /// 连接所有设备。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>操作结果。</returns>
    Task<OperateResult> ConnectAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 断开所有设备。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>操作结果。</returns>
    Task<OperateResult> DisconnectAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取设备状态。
    /// </summary>
    /// <param name="deviceId">设备 ID。</param>
    /// <returns>连接状态。</returns>
    ConnectionState GetDeviceState(string deviceId);

    /// <summary>
    /// 设备状态变更事件。
    /// </summary>
    event EventHandler<DeviceStateChangedEventArgs>? AnyDeviceStateChanged;
}
