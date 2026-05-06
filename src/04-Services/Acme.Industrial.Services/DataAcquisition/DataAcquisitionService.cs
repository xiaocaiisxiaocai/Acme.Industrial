using Acme.Industrial.Communication.Abstractions;
using Acme.Industrial.Communication.Abstractions.Enumerations;
using Acme.Industrial.Communication.Abstractions.Models;
using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Acme.Industrial.Services.DataAcquisition;

/// <summary>
/// 数据采集服务实现
/// </summary>
public class DataAcquisitionService : IDataAcquisitionService
{
    private readonly IDeviceManager _deviceManager;
    private readonly ITagSubscriber _tagSubscriber;
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new();
    private readonly DataAcqStatistics _statistics = new();
    private bool _disposed;

    public DataAcquisitionService(
        IDeviceManager deviceManager,
        ITagSubscriber tagSubscriber,
        IAppLogger logger)
    {
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _tagSubscriber = tagSubscriber ?? throw new ArgumentNullException(nameof(tagSubscriber));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDataAcquisitionStatistics Statistics => _statistics;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.Info("数据采集服务初始化完成");
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(Tag[] tags, Action<TagValue> callback)
    {
        var subscription = _tagSubscriber.Subscribe(tags, callback);
        var id = Guid.NewGuid().ToString();
        _subscriptions[id] = subscription;
        return new SubscriptionGuard(() => _subscriptions.TryRemove(id, out _));
    }

    public IDisposable Subscribe(string deviceId, string[] addresses, Action<TagValue> callback)
    {
        var tags = addresses.Select((addr, idx) => new Tag
        {
            Name = deviceId + "." + addr,
            Address = addr,
            DataType = DataType.Float,
            ScanRate = 1000
        }).ToArray();

        return Subscribe(tags, callback);
    }

    public async Task<Dictionary<string, TagValue>> ReadBatchAsync(
        string deviceId, string[] addresses, CancellationToken ct = default)
    {
        var result = new Dictionary<string, TagValue>();

        if (addresses.Length == 0)
            return result;

        var sw = Stopwatch.StartNew();

        try
        {
            var device = _deviceManager.GetDevice(deviceId);
            if (device == null)
            {
                _logger.Warn("设备 " + deviceId + " 不存在");
                _statistics.RecordFailed(addresses.Length);
                return result;
            }

            var tags = addresses.Select(addr => new Tag
            {
                Name = deviceId + "." + addr,
                Address = addr,
                DataType = DataType.Float
            }).ToArray();

            var readResult = await device.ReadBatchAsync(tags, ct);

            if (readResult.IsSuccess && readResult.Content != null)
            {
                foreach (var tv in readResult.Content)
                {
                    var address = tv.TagName.Contains('.')
                        ? tv.TagName.Split('.').Last()
                        : tv.TagName;
                    result[address] = tv;
                }
                _statistics.RecordSuccess(addresses.Length);
            }
            else
            {
                _logger.Warn("批量读取失败: " + readResult.Message);
                _statistics.RecordFailed(addresses.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("批量读取异常: " + ex.Message, ex);
            _statistics.RecordFailed(addresses.Length);
        }

        sw.Stop();
        _statistics.RecordLatency(sw.ElapsedMilliseconds);
        return result;
    }

    public async Task<OperateResult<TagValue>> ReadAsync(
        string deviceId, string address, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var device = _deviceManager.GetDevice(deviceId);
        if (device == null)
        {
            _statistics.RecordFailed(1);
            return MakeFail<TagValue>(-1, "设备 " + deviceId + " 不存在");
        }

        var tag = new Tag
        {
            Name = deviceId + "." + address,
            Address = address,
            DataType = DataType.Float
        };

        try
        {
            var result = await ReadTagAsync(device, tag, ct);
            sw.Stop();
            _statistics.RecordLatency(sw.ElapsedMilliseconds);

            if (result.IsSuccess)
            {
                _statistics.RecordSuccess(1);
            }
            else
            {
                _statistics.RecordFailed(1);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error("读取点位异常: " + deviceId + "/" + address + " - " + ex.Message, ex);
            _statistics.RecordFailed(1);
            return MakeFail<TagValue>(-1, ex.Message);
        }
    }

    private static async Task<OperateResult<TagValue>> ReadTagAsync(
        IDeviceDriver device, Tag tag, CancellationToken ct)
    {
        // 获取字节长度
        var length = GetDataLength(tag.DataType);
        var raw = await device.ReadRawAsync(tag.Address, (ushort)length, ct);

        if (!raw.IsSuccess || raw.Content == null)
        {
            return MakeFail<TagValue>(raw.ErrorCode, raw.Message);
        }

        var value = ParseRawValue(tag.DataType, raw.Content);
        return MakeOk(new TagValue
        {
            TagName = tag.Name,
            Value = value,
            Quality = TagQuality.Good,
            Timestamp = DateTime.Now,
            RawBytes = raw.Content
        });
    }

    private static int GetDataLength(DataType dataType)
    {
        return dataType switch
        {
            DataType.Bool => 1,
            DataType.Byte => 1,
            DataType.Int16 => 2,
            DataType.UInt16 => 2,
            DataType.Int32 => 4,
            DataType.UInt32 => 4,
            DataType.Int64 => 8,
            DataType.UInt64 => 8,
            DataType.Float => 4,
            DataType.Double => 8,
            DataType.String => 20,
            _ => 4
        };
    }

    private static object? ParseRawValue(DataType dataType, byte[] data)
    {
        if (data.Length < 4) return BitConverter.ToSingle(data, 0);

        return dataType switch
        {
            DataType.Bool => data[0] != 0,
            DataType.Byte => data[0],
            DataType.Int16 => BitConverter.ToInt16(data, 0),
            DataType.UInt16 => BitConverter.ToUInt16(data, 0),
            DataType.Int32 => BitConverter.ToInt32(data, 0),
            DataType.UInt32 => BitConverter.ToUInt32(data, 0),
            DataType.Int64 => BitConverter.ToInt64(data, 0),
            DataType.UInt64 => BitConverter.ToUInt64(data, 0),
            DataType.Float => BitConverter.ToSingle(data, 0),
            DataType.Double => BitConverter.ToDouble(data, 0),
            DataType.String => System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0'),
            _ => BitConverter.ToSingle(data, 0)
        };
    }

    private static OperateResult<T> MakeOk<T>(T content)
    {
        return new OperateResult<T>
        {
            IsSuccess = true,
            ErrorCode = 0,
            Message = string.Empty,
            Content = content
        };
    }

    private static OperateResult<T> MakeFail<T>(int errorCode, string message)
    {
        return new OperateResult<T>
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            Message = message
        };
    }

    public async Task<OperateResult> WriteAsync(
        string deviceId, string address, object value, CancellationToken ct = default)
    {
        var device = _deviceManager.GetDevice(deviceId);
        if (device == null)
        {
            return OperateResult.Fail(-1, "设备 " + deviceId + " 不存在");
        }

        var tag = new Tag
        {
            Name = deviceId + "." + address,
            Address = address,
            DataType = DataType.Float
        };

        try
        {
            return await device.WriteAsync(tag, value, ct);
        }
        catch (Exception ex)
        {
            _logger.Error("写入点位异常: " + deviceId + "/" + address + " - " + ex.Message, ex);
            return OperateResult.Fail(-1, ex.Message);
        }
    }

    public async Task<OperateResult> ConnectDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var device = _deviceManager.GetDevice(deviceId);
        if (device == null)
        {
            return OperateResult.Fail(-1, "设备 " + deviceId + " 不存在");
        }

        try
        {
            return await device.ConnectAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.Error("连接设备异常: " + deviceId + " - " + ex.Message, ex);
            return OperateResult.Fail(-1, ex.Message);
        }
    }

    public async Task<OperateResult> DisconnectDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var device = _deviceManager.GetDevice(deviceId);
        if (device == null)
        {
            return OperateResult.Fail(-1, "设备 " + deviceId + " 不存在");
        }

        try
        {
            await device.DisconnectAsync(ct);
            return OperateResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error("断开设备异常: " + deviceId + " - " + ex.Message, ex);
            return OperateResult.Fail(-1, ex.Message);
        }
    }

    public ConnectionState GetDeviceState(string deviceId)
    {
        var device = _deviceManager.GetDevice(deviceId);
        return device?.State ?? ConnectionState.Disconnected;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var sub in _subscriptions.Values)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();

        GC.SuppressFinalize(this);
    }

    private class DataAcqStatistics : IDataAcquisitionStatistics
    {
        private long _totalReads;
        private long _successReads;
        private long _failedReads;
        private long _totalLatencyMs;
        private long _latencyCount;

        public long TotalReads => _totalReads;
        public long SuccessReads => _successReads;
        public long FailedReads => _failedReads;
        public double ReadSuccessRate => _totalReads == 0 ? 0 : (double)_successReads / _totalReads * 100;
        public double AverageLatencyMs => _latencyCount == 0 ? 0 : (double)_totalLatencyMs / _latencyCount;

        public void RecordSuccess(int count)
        {
            Interlocked.Add(ref _totalReads, count);
            Interlocked.Add(ref _successReads, count);
        }

        public void RecordFailed(int count)
        {
            Interlocked.Add(ref _totalReads, count);
            Interlocked.Add(ref _failedReads, count);
        }

        public void RecordLatency(long latencyMs)
        {
            Interlocked.Add(ref _totalLatencyMs, latencyMs);
            Interlocked.Increment(ref _latencyCount);
        }
    }

    private class SubscriptionGuard : IDisposable
    {
        private readonly Action _cleanup;
        private bool _disposed;

        public SubscriptionGuard(Action cleanup) => _cleanup = cleanup;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cleanup();
            }
        }
    }
}
