using System.Collections.Concurrent;
using System.Text;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Communication.Mock;

public class MockDeviceDriver : DeviceDriverBase
{
    private readonly ConcurrentDictionary<string, byte[]> _memory = new();
    private readonly Random _rand = new();

    public int SimulatedDelayMs { get; set; } = 10;
    public double ErrorRate { get; set; } = 0;

    public MockDeviceDriver(ConnectionOptions opts, IAppLoggerFactory lf, IByteTransform bt)
        : base(opts, lf, bt)
    {
        _memory["40001"] = BitConverter.GetBytes((short)100);
        _memory["40002"] = BitConverter.GetBytes((short)200);
        _memory["40003"] = BitConverter.GetBytes((short)300);
    }

    protected override Task<OperateResult> ConnectCoreAsync(CancellationToken ct)
        => Task.FromResult(OperateResult.Ok());

    protected override Task<OperateResult> DisconnectCoreAsync(CancellationToken ct)
        => Task.FromResult(OperateResult.Ok());

    protected override async Task<OperateResult<byte[]>> ReadRawCoreAsync(
        string address, ushort length, CancellationToken ct)
    {
        await Task.Delay(SimulatedDelayMs, ct);
        if (_rand.NextDouble() < ErrorRate)
            return OperateResult.Fail<byte[]>(ErrorCode.CommReadFailed, "Simulated error");

        var data = _memory.GetOrAdd(address, _ => new byte[length]);
        return OperateResult.Ok(data.Take(length).ToArray());
    }

    protected override async Task<OperateResult> WriteRawCoreAsync(
        string address, byte[] data, CancellationToken ct)
    {
        await Task.Delay(SimulatedDelayMs, ct);
        if (_rand.NextDouble() < ErrorRate)
            return OperateResult.Fail(ErrorCode.CommWriteFailed, "Simulated error");

        _memory[address] = data;
        return OperateResult.Ok();
    }

    protected override Task<OperateResult> PingCoreAsync(CancellationToken ct)
        => Task.FromResult(OperateResult.Ok());

    protected override (ushort byteLength, ushort registerCount) GetByteLength(Tag tag) => tag.DataType switch
    {
        DataType.Bool or DataType.Byte or DataType.SByte => (1, 1),
        DataType.Int16 or DataType.UInt16 => (2, 1),
        DataType.Int32 or DataType.UInt32 or DataType.Float => (4, 2),
        DataType.Int64 or DataType.UInt64 or DataType.Double => (8, 4),
        DataType.String or DataType.ByteArray => ((ushort)tag.Length, (ushort)((tag.Length + 1) / 2)),
        _ => (2, 1)
    };

    protected override object ParseValue(Tag tag, byte[] bytes) => tag.DataType switch
    {
        DataType.Bool => ByteTransform.ToBool(bytes, 0),
        DataType.Int16 => ByteTransform.ToInt16(bytes, 0),
        DataType.UInt16 => ByteTransform.ToUInt16(bytes, 0),
        DataType.Int32 => ByteTransform.ToInt32(bytes, 0),
        DataType.UInt32 => ByteTransform.ToUInt32(bytes, 0),
        DataType.Float => ByteTransform.ToFloat(bytes, 0),
        DataType.Double => ByteTransform.ToDouble(bytes, 0),
        DataType.String => ByteTransform.ToString(bytes, 0, bytes.Length, Encoding.ASCII),
        _ => bytes
    };

    protected override byte[] SerializeValue(Tag tag, object value) => tag.DataType switch
    {
        DataType.Bool => ByteTransform.FromBool((bool)value),
        DataType.Int16 => ByteTransform.FromInt16(Convert.ToInt16(value)),
        DataType.UInt16 => ByteTransform.FromUInt16(Convert.ToUInt16(value)),
        DataType.Int32 => ByteTransform.FromInt32(Convert.ToInt32(value)),
        DataType.UInt32 => ByteTransform.FromUInt32(Convert.ToUInt32(value)),
        DataType.Float => ByteTransform.FromFloat(Convert.ToSingle(value)),
        DataType.Double => ByteTransform.FromDouble(Convert.ToDouble(value)),
        DataType.String => ByteTransform.FromString((string)value, Encoding.ASCII),
        _ => (byte[])value
    };

    protected override DataType ResolveDataType<T>() => typeof(T) switch
    {
        var t when t == typeof(bool) => DataType.Bool,
        var t when t == typeof(byte) => DataType.Byte,
        var t when t == typeof(sbyte) => DataType.SByte,
        var t when t == typeof(short) => DataType.Int16,
        var t when t == typeof(ushort) => DataType.UInt16,
        var t when t == typeof(int) => DataType.Int32,
        var t when t == typeof(uint) => DataType.UInt32,
        var t when t == typeof(long) => DataType.Int64,
        var t when t == typeof(ulong) => DataType.UInt64,
        var t when t == typeof(float) => DataType.Float,
        var t when t == typeof(double) => DataType.Double,
        var t when t == typeof(string) => DataType.String,
        _ => DataType.Int16
    };

    public void SetRegister(string address, short value)
    {
        _memory[address] = BitConverter.GetBytes(value);
    }

    public short GetRegister(string address)
    {
        if (_memory.TryGetValue(address, out var bytes) && bytes.Length >= 2)
            return BitConverter.ToInt16(bytes, 0);
        return 0;
    }
}