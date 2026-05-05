using System.Collections.Concurrent;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Core;

/// <summary>
/// 设备管理器实现 - 统一管理多个设备驱动。
/// </summary>
public class DeviceManager : IDeviceManager
{
    private readonly ConcurrentDictionary<string, IDeviceDriver> _devices = new();
    private readonly IDriverFactory _driverFactory;
    private readonly IAppLogger _logger;
    private bool _disposed;

    /// <summary>
    /// 构造函数。
    /// </summary>
    public DeviceManager(IDriverFactory driverFactory, IAppLoggerFactory loggerFactory)
    {
        _driverFactory = driverFactory ?? throw new ArgumentNullException(nameof(driverFactory));
        _logger = loggerFactory?.CreateLogger(nameof(DeviceManager))
            ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// 添加设备。
    /// </summary>
    public async Task<OperateResult> AddDeviceAsync(ConnectionOptions options, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(options.DeviceId))
            return OperateResult.Fail(ErrorCode.InvalidArgument, "设备 ID 不能为空");

        if (!_driverFactory.IsProtocolSupported(options.Protocol))
            return OperateResult.Fail(ErrorCode.CommProtocolError, $"不支持的协议: {options.Protocol}");

        if (_devices.ContainsKey(options.DeviceId))
            return OperateResult.Fail(ErrorCode.BizConflict, $"设备已存在: {options.DeviceId}");

        try
        {
            var driver = _driverFactory.Create(options);
            driver.StateChanged += OnDeviceStateChanged;

            if (!_devices.TryAdd(options.DeviceId, driver))
            {
                driver.StateChanged -= OnDeviceStateChanged;
                return OperateResult.Fail(ErrorCode.BizConflict, $"设备添加失败: {options.DeviceId}");
            }

            _logger.Info($"设备已添加: {options.DeviceId} ({options.Protocol})");
            return OperateResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error($"设备添加失败: {options.DeviceId}", ex);
            return OperateResult.Fail(ErrorCode.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// 移除设备。
    /// </summary>
    public async Task<OperateResult> RemoveDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (!_devices.TryRemove(deviceId, out var driver))
            return OperateResult.Fail(ErrorCode.BizNotFound, $"设备不存在: {deviceId}");

        try
        {
            driver.StateChanged -= OnDeviceStateChanged;

            if (driver.State == ConnectionState.Connected)
            {
                await driver.DisconnectAsync(ct);
            }

            if (driver is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }

            _logger.Info($"设备已移除: {deviceId}");
            return OperateResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error($"设备移除失败: {deviceId}", ex);
            return OperateResult.Fail(ErrorCode.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// 获取设备驱动。
    /// </summary>
    public IDeviceDriver? GetDevice(string deviceId)
    {
        ThrowIfDisposed();
        return _devices.GetValueOrDefault(deviceId);
    }

    /// <summary>
    /// 获取所有设备驱动。
    /// </summary>
    public IReadOnlyList<IDeviceDriver> GetAllDevices()
    {
        ThrowIfDisposed();
        return _devices.Values.ToList();
    }

    /// <summary>
    /// 连接所有设备。
    /// </summary>
    public async Task<OperateResult> ConnectAllAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var tasks = _devices.Values.Select(d => ConnectWithErrorHandle(d, ct)).ToList();
        var results = await Task.WhenAll(tasks);

        var failed = results.Where(r => !r.IsSuccess).ToList();
        if (failed.Count > 0)
        {
            var errors = string.Join("; ", failed.Select(r => r.Message));
            return OperateResult.Fail(ErrorCode.CommConnectFailed, $"部分设备连接失败: {errors}");
        }

        return OperateResult.Ok();
    }

    /// <summary>
    /// 断开所有设备。
    /// </summary>
    public async Task<OperateResult> DisconnectAllAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var tasks = _devices.Values.Select(d => d.DisconnectAsync(ct)).ToList();
        await Task.WhenAll(tasks);

        _logger.Info("所有设备已断开");
        return OperateResult.Ok();
    }

    /// <summary>
    /// 获取设备状态。
    /// </summary>
    public ConnectionState GetDeviceState(string deviceId)
    {
        ThrowIfDisposed();
        return _devices.GetValueOrDefault(deviceId)?.State ?? ConnectionState.Disconnected;
    }

    /// <summary>
    /// 设备状态变更事件。
    /// </summary>
    public event EventHandler<DeviceStateChangedEventArgs>? AnyDeviceStateChanged;

    private async Task<OperateResult> ConnectWithErrorHandle(IDeviceDriver driver, CancellationToken ct)
    {
        try
        {
            return await driver.ConnectAsync(ct);
        }
        catch (Exception ex)
        {
            return OperateResult.Fail(ErrorCode.CommConnectFailed, ex.Message);
        }
    }

    private void OnDeviceStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        if (sender is not IDeviceDriver driver) return;

        _logger.Info($"设备状态变更: {driver.DeviceId} -> {e.NewState}");

        AnyDeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
        {
            DeviceId = driver.DeviceId,
            OldState = e.OldState,
            NewState = e.NewState,
            Message = e.Message
        });
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DeviceManager));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var driver in _devices.Values)
        {
            driver.StateChanged -= OnDeviceStateChanged;
            if (driver.State == ConnectionState.Connected)
            {
                await driver.DisconnectAsync();
            }

            if (driver is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }

        _devices.Clear();
        GC.SuppressFinalize(this);
    }
}
