using System.Net.Sockets;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Communication.Siemens;

/// <summary>
/// 西门子 S7 驱动（简化版，支持 S7-1200/1500 ISO-over-TCP）。
/// </summary>
public class SiemensS7Driver : DeviceDriverBase
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _tid = 1;

    public SiemensS7Driver(ConnectionOptions opts, IAppLoggerFactory lf, IByteTransform bt)
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

            // S7 RFC1006 ISO handshake
            var connectPdu = BuildConnectPdu();
            await _stream.WriteAsync(connectPdu, ct);
            var resp = new byte[1024];
            var len = await _stream.ReadAsync(resp, ct);
            if (len < 22 || resp[21] != 0x00)
                return OperateResult.Fail(ErrorCode.CommConnectFailed, "S7 handshake failed");

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
        var result = await ReadRawCoreAsync("DB1.0", 2, ct);
        return result.IsSuccess
            ? OperateResult.Ok()
            : OperateResult.Fail(result.ErrorCode, result.Message);
    }

    protected override async Task<OperateResult<byte[]>> ReadRawCoreAsync(
        string address, ushort length, CancellationToken ct)
    {
        if (_stream == null)
            return OperateResult.Fail<byte[]>(ErrorCode.CommNotConnected, "Not connected");

        try
        {
            var request = BuildReadRequest(address, length);
            await _stream.WriteAsync(request, ct);
            var response = await ReadPduAsync(ct);
            if (!response.IsSuccess)
                return OperateResult.Fail<byte[]>(response);

            // 解析 S7 Read Response
            var data = ParseReadResponse(response.Content!);
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

        try
        {
            var request = BuildWriteRequest(address, data);
            await _stream.WriteAsync(request, ct);
            var response = await ReadPduAsync(ct);
            return response.IsSuccess
                ? OperateResult.Ok()
                : OperateResult.Fail(response);
        }
        catch (Exception ex)
        {
            return OperateResult.Fail(ErrorCode.CommWriteFailed, ex.Message);
        }
    }

    private async Task<OperateResult<byte[]>> ReadPduAsync(CancellationToken ct)
    {
        var header = new byte[4];
        var totalRead = 0;
        var deadline = DateTime.Now.AddMilliseconds(Options.ReadTimeoutMs);

        while (totalRead < 4 && DateTime.Now < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (_stream!.DataAvailable)
            {
                totalRead += await _stream.ReadAsync(header.AsMemory(totalRead, 4 - totalRead), ct);
            }
            else
            {
                await Task.Delay(10, ct);
            }
        }

        if (totalRead < 4)
            return OperateResult.Fail<byte[]>(ErrorCode.CommTimeout, "Header timeout");

        var pduLen = (header[2] << 8) | header[3];
        var pdu = new byte[pduLen + 4];
        Array.Copy(header, pdu, 4);

        totalRead = 4;
        deadline = DateTime.Now.AddMilliseconds(Options.ReadTimeoutMs);
        while (totalRead < pduLen + 4 && DateTime.Now < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (_stream!.DataAvailable)
            {
                totalRead += await _stream.ReadAsync(pdu.AsMemory(totalRead, pdu.Length - totalRead), ct);
            }
            else
            {
                await Task.Delay(10, ct);
            }
        }

        return OperateResult.Ok(pdu);
    }

    private static byte[] BuildConnectPdu()
    {
        return new byte[]
        {
            // RFC1006 Header
            0x03, 0x00, 0x00, 0x19,           // Length = 25
            0x11,                             // RFC1006 type (connection request)
            0xE0,                             // Destination reference
            0x00, 0x00,                       // Source reference
            0x00,                             // Class option
            // S7 Header
            0x01,                             // PDU Type: Setup connection
            0x00, 0x00,                       // Reserved
            0x01, 0x00,                       // Protocol ID
            0x0C,                             // Data length
            0x01,                             // Parameter length
            0x00,                             // Return code
            0xC0, 0x01, 0x0A,                // COTP dest reference
            0xC0, 0x01, 0x0B,                // COTP src reference
            0x02,                             // COTP class
            // S7 Connection Request
            0x01,                             // Function: Setup
            0x00, 0x00,                       // Reserved
            0x00, 0x00,                       // PDU length request
            0x03, 0x00,                       // PDU length
        };
    }

    private byte[] BuildReadRequest(string address, ushort length)
    {
        var addr = ParseS7Address(address);
        var pdu = new List<byte>();

        // S7 Header
        pdu.AddRange(new byte[] { 0x03, 0x00 });           // Protocol ID
        pdu.AddRange(new byte[] { 0x00, 0x00 });           // Will be overwritten with length
        pdu.Add(0x02);                                    // PDU Type: Job
        pdu.AddRange(new byte[] { 0x01, 0x00 });          // Reserved
        pdu.Add((byte)(_tid >> 8)); pdu.Add((byte)_tid);  // Transaction ID
        pdu.AddRange(new byte[] { 0x00, 0x0E });          // Parameter length = 14
        pdu.Add(0x00);                                    // Data length = 0

        // Parameter: Read Var
        pdu.Add(0x04);                                    // Function: Read Var
        pdu.Add(0x01);                                    // Item count

        // Item
        pdu.Add(addr.Area);                               // Area code
        pdu.Add((byte)(addr.DbNumber >> 8)); pdu.Add((byte)addr.DbNumber); // DB number
        pdu.Add(addr.DataType);                           // 0x03 = bytes, 0x02 = bit
        pdu.Add((byte)(addr.Offset >> 16)); pdu.Add((byte)(addr.Offset >> 8)); pdu.Add((byte)addr.Offset);
        pdu.Add((byte)(length >> 8)); pdu.Add((byte)length);  // Length in bits/bytes

        // Fix length in header
        pdu[4] = (byte)((pdu.Count - 4) >> 8);
        pdu[5] = (byte)(pdu.Count - 4);

        _tid++;
        return pdu.ToArray();
    }

    private byte[] BuildWriteRequest(string address, byte[] data)
    {
        var addr = ParseS7Address(address);
        var pdu = new List<byte>();

        // S7 Header
        pdu.AddRange(new byte[] { 0x03, 0x00 });
        pdu.AddRange(new byte[] { 0x00, 0x00 });
        pdu.Add(0x02);                                    // PDU Type: Job
        pdu.AddRange(new byte[] { 0x01, 0x00 });
        pdu.Add((byte)(_tid >> 8)); pdu.Add((byte)_tid);
        pdu.AddRange(new byte[] { 0x00, 0x0E });          // Parameter length
        pdu.Add((byte)((data.Length + 4) >> 8)); pdu.Add((byte)(data.Length + 4)); // Data length

        // Parameter: Write Var
        pdu.Add(0x05);                                    // Function: Write Var
        pdu.Add(0x01);                                    // Item count

        // Item
        pdu.Add(addr.Area);
        pdu.Add((byte)(addr.DbNumber >> 8)); pdu.Add((byte)addr.DbNumber);
        pdu.Add(addr.DataType);
        pdu.Add((byte)(addr.Offset >> 16)); pdu.Add((byte)(addr.Offset >> 8)); pdu.Add((byte)addr.Offset);
        pdu.Add((byte)(data.Length >> 8)); pdu.Add((byte)data.Length);

        // Data
        pdu.Add(0x00);                                    // Transport size = byte
        pdu.Add((byte)(data.Length >> 8)); pdu.Add((byte)data.Length);
        pdu.AddRange(data);

        // Fix length
        pdu[4] = (byte)((pdu.Count - 4) >> 8);
        pdu[5] = (byte)(pdu.Count - 4);

        _tid++;
        return pdu.ToArray();
    }

    private static S7Address ParseS7Address(string address)
    {
        // 格式: DB1.DBD0, DB1.DBW0, DB1.DBB0, M0.0, I0.0, Q0.0, etc.
        var result = new S7Address();

        if (address.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
        {
            var parts = address.Split('.');
            if (parts.Length >= 2)
            {
                result.DbNumber = ushort.TryParse(parts[0].Substring(2), out var db) ? db : (ushort)1;
                result.Area = 0x84;  // DB
                if (parts[1].StartsWith("DBD", StringComparison.OrdinalIgnoreCase))
                {
                    result.Offset = uint.TryParse(parts[1].Substring(3), out var off) ? off : 0;
                    result.DataType = 0x03; // bytes
                }
                else if (parts[1].StartsWith("DBW", StringComparison.OrdinalIgnoreCase))
                {
                    result.Offset = uint.TryParse(parts[1].Substring(3), out var off) ? off : 0;
                    result.DataType = 0x03;
                }
                else if (parts[1].StartsWith("DBB", StringComparison.OrdinalIgnoreCase))
                {
                    result.Offset = uint.TryParse(parts[1].Substring(3), out var off) ? off : 0;
                    result.DataType = 0x03;
                }
                else
                {
                    result.Offset = uint.TryParse(parts[1], out var off) ? off : 0;
                    result.DataType = 0x03;
                }
            }
        }
        else if (address.StartsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            result.Area = 0x83; // Markers
            result.Offset = uint.TryParse(address.Substring(1), out var off) ? off : 0;
            result.DataType = 0x03;
        }
        else if (address.StartsWith("I", StringComparison.OrdinalIgnoreCase) || address.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
        {
            result.Area = address.StartsWith("I") ? (byte)0x81 : (byte)0x82; // Inputs or Outputs
            result.Offset = uint.TryParse(address.Substring(1), out var off) ? off : 0;
            result.DataType = 0x03;
        }

        return result;
    }

    private static byte[] ParseReadResponse(byte[] pdu)
    {
        if (pdu.Length < 21 || pdu[7] != 0x04)
            return Array.Empty<byte>();

        var dataLen = (pdu[20] << 8) | pdu[21];
        var data = new byte[dataLen];
        Array.Copy(pdu, 22, data, 0, dataLen);
        return data;
    }

    protected override (ushort byteLength, ushort registerCount) GetByteLength(Tag tag) => tag.DataType switch
    {
        DataType.Bool => (1, 1),
        DataType.Byte or DataType.SByte => (1, 1),
        DataType.Int16 or DataType.UInt16 => (2, 1),
        DataType.Int32 or DataType.UInt32 or DataType.Float => (4, 2),
        DataType.Int64 or DataType.UInt64 or DataType.Double => (8, 4),
        DataType.String or DataType.ByteArray => ((ushort)tag.Length, (ushort)tag.Length),
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
        DataType.String => ByteTransform.ToString(bytes, 0, bytes.Length, System.Text.Encoding.UTF8),
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
        DataType.String => ByteTransform.FromString((string)value, System.Text.Encoding.UTF8),
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

    private struct S7Address
    {
        public byte Area;
        public ushort DbNumber;
        public byte DataType;
        public uint Offset;
    }
}
