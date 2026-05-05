using System.Net.Sockets;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Modbus;

/// <summary>
/// Modbus TCP 驱动。
/// </summary>
public class ModbusTcpDriver : DeviceDriverBase
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public ModbusTcpDriver(ConnectionOptions opts, IAppLoggerFactory lf, IByteTransform bt)
        : base(opts, lf, bt)
    {
    }

    protected override async Task<OperateResult> ConnectCoreAsync(CancellationToken ct)
    {
        try
        {
            _client = new TcpClient
            {
                ReceiveTimeout = Options.ReadTimeoutMs,
                SendTimeout = Options.WriteTimeoutMs
            };
            await _client.ConnectAsync(Options.Host!, Options.Port, ct);
            _stream = _client.GetStream();
            return OperateResult.Ok();
        }
        catch (Exception ex)
        {
            return OperateResult.Fail(ErrorCode.CommConnectFailed, ex.Message);
        }
    }

    protected override async Task<OperateResult> DisconnectCoreAsync(CancellationToken ct)
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        await Task.CompletedTask;
        return OperateResult.Ok();
    }

    protected override async Task<OperateResult> PingCoreAsync(CancellationToken ct)
    {
        var result = await ReadRawCoreAsync("40001", 2, ct);
        return result.IsSuccess
            ? OperateResult.Ok()
            : OperateResult.Fail(result.ErrorCode, result.Message);
    }

    protected override async Task<OperateResult<byte[]>> ReadRawCoreAsync(
        string address, ushort length, CancellationToken ct)
    {
        if (_stream == null)
            return OperateResult.Fail<byte[]>(ErrorCode.CommNotConnected, "Not connected");

        var (startAddress, funcCode) = ParseAddress(address);

        byte[] request;
        if (funcCode switch
        {
            >= 1 and <= 4 => true,  // Read Coils/Discretes/Registers
            _ => false
        })
        {
            // 读请求
            request = BuildReadRequest(funcCode, startAddress, length);
        }
        else
        {
            return OperateResult.Fail<byte[]>(ErrorCode.CommAddressInvalid, "Invalid address");
        }

        try
        {
            await _stream.WriteAsync(request, ct);
            var response = await ReadResponseAsync(5 + length, ct);
            if (!response.IsSuccess)
                return OperateResult.Fail<byte[]>(response);

            var data = response.Content!
                .Skip(9)  // skip header + func code + byte count
                .ToArray();

            return OperateResult.Ok(data);
        }
        catch (Exception ex)
        {
            return OperateResult.Fail<byte[]>(ErrorCode.CommReadFailed, ex.Message);
        }
    }

    protected override async Task<OperateResult> WriteRawCoreAsync(
        string address, byte[] data, CancellationToken ct)
    {
        if (_stream == null)
            return OperateResult.Fail(ErrorCode.CommNotConnected, "Not connected");

        var (startAddress, funcCode) = ParseAddress(address);

        var request = new byte[14 + data.Length];
        ushort tid = (ushort)Random.Shared.Next(1, 65535);

        // MBAP Header
        request[0] = (byte)(tid >> 8);   // Transaction ID
        request[1] = (byte)(tid);
        request[2] = 0;                   // Protocol ID
        request[3] = 0;
        request[4] = (byte)((7 + data.Length) >> 8);  // Length
        request[5] = (byte)(7 + data.Length);
        request[6] = Options.SlaveAddress; // Unit ID

        // Function code: Write Multiple Registers = 16
        request[7] = 16;
        request[8] = (byte)(startAddress >> 8);
        request[9] = (byte)startAddress;
        request[10] = (byte)(data.Length >> 9);  // Quantity
        request[11] = (byte)(data.Length / 2);
        request[12] = (byte)data.Length;         // Byte count

        Array.Copy(data, 0, request, 13, data.Length);

        try
        {
            await _stream.WriteAsync(request, ct);
            var response = await ReadResponseAsync(12, ct);
            return response.IsSuccess
                ? OperateResult.Ok()
                : OperateResult.Fail(response);
        }
        catch (Exception ex)
        {
            return OperateResult.Fail(ErrorCode.CommWriteFailed, ex.Message);
        }
    }

    private async Task<OperateResult<byte[]>> ReadResponseAsync(int expectedBytes, CancellationToken ct)
    {
        var buffer = new byte[256];
        var totalRead = 0;
        var deadline = DateTime.Now.AddMilliseconds(Options.ReadTimeoutMs);

        while (totalRead < expectedBytes && DateTime.Now < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var available = _stream!.DataAvailable
                ? Math.Min(expectedBytes - totalRead, _stream.Read(buffer, totalRead, Math.Min(256 - totalRead, expectedBytes - totalRead)))
                : 0;

            if (available == 0)
            {
                await Task.Delay(10, ct);
                continue;
            }
            totalRead += available;
        }

        if (totalRead < expectedBytes)
            return OperateResult.Fail<byte[]>(ErrorCode.CommTimeout, "Response timeout");

        return OperateResult.Ok(buffer.Take(totalRead).ToArray());
    }

    private static (ushort startAddress, byte funcCode) ParseAddress(string address)
    {
        // 地址格式: 4XXXX 或 3XXXX 或 0XXXX 或 1XXXX
        if (address.Length < 2) return (0, 3);

        var prefix = address[0] switch
        {
            '4' => 3,   // Holding Register -> Function Code 3
            '3' => 4,   // Input Register -> Function Code 4
            '0' => 1,   // Coil -> Function Code 1
            '1' => 2,   // Discrete Input -> Function Code 2
            _ => 3
        };

        var regAddr = ushort.TryParse(address.AsSpan().Slice(1), out var addr) ? addr : (ushort)0;
        return (regAddr, (byte)prefix);
    }

    private byte[] BuildReadRequest(byte funcCode, ushort startAddress, ushort quantity)
    {
        var request = new byte[12];
        ushort tid = (ushort)Random.Shared.Next(1, 65535);

        request[0] = (byte)(tid >> 8);
        request[1] = (byte)tid;
        request[2] = 0;
        request[3] = 0;
        request[4] = 0;
        request[5] = 6;                   // 后面 6 字节
        request[6] = Options.SlaveAddress;
        request[7] = funcCode;
        request[8] = (byte)(startAddress >> 8);
        request[9] = (byte)startAddress;
        request[10] = (byte)(quantity >> 8);
        request[11] = (byte)quantity;

        return request;
    }

    protected override (ushort byteLength, ushort registerCount) GetByteLength(Tag tag) => tag.DataType switch
    {
        DataType.Bool => (1, 1),
        DataType.Byte or DataType.SByte => (1, 1),
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
        DataType.String => ByteTransform.ToString(bytes, 0, bytes.Length, System.Text.Encoding.ASCII),
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
        DataType.String => ByteTransform.FromString((string)value, System.Text.Encoding.ASCII),
        _ => (byte[])value
    };

    protected override DataType ResolveDataType<T>() => typeof(T) switch
    {
        var t when t == typeof(bool) => DataType.Bool,
        var t when t == typeof(short) => DataType.Int16,
        var t when t == typeof(ushort) => DataType.UInt16,
        var t when t == typeof(int) => DataType.Int32,
        var t when t == typeof(uint) => DataType.UInt32,
        var t when t == typeof(float) => DataType.Float,
        var t when t == typeof(double) => DataType.Double,
        var t when t == typeof(string) => DataType.String,
        _ => DataType.Int16
    };
}
