using System.Net.Sockets;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Mitsubishi;

/// <summary>
/// 三菱 MC 协议驱动（支持 QnA/3E 帧，通过 TCP）。
/// 支持三菱 Q 系列、 iQ-R 系列 PLC。
/// </summary>
public class MitsubishiMcDriver : DeviceDriverBase
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _tid = 1;
    private readonly object _tidLock = new();

    /// <summary>
    /// 网络超时时间（毫秒）。
    /// </summary>
    public int NetworkTimeoutMs { get; set; } = 5000;

    public MitsubishiMcDriver(ConnectionOptions opts, IAppLoggerFactory lf, IByteTransform bt)
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

            // MC 协议无需特殊握手，直接测试读取即可
            var testResult = await ReadRawCoreAsync("D0", 2, ct);
            if (!testResult.IsSuccess)
            {
                return OperateResult.Fail(ErrorCode.CommConnectFailed, "MC 连接测试失败");
            }

            return OperateResult.Ok();
        }
        catch (Exception ex)
        {
            return OperateResult.Fail(ErrorCode.CommConnectFailed, ex.Message);
        }
    }

    protected override Task<OperateResult> DisconnectCoreAsync(CancellationToken ct)
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        return Task.FromResult(OperateResult.Ok());
    }

    protected override async Task<OperateResult> PingCoreAsync(CancellationToken ct)
    {
        var result = await ReadRawCoreAsync("D0", 2, ct);
        return result.IsSuccess
            ? OperateResult.Ok()
            : OperateResult.Fail(result.ErrorCode, result.Message);
    }

    protected override async Task<OperateResult<byte[]>> ReadRawCoreAsync(
        string address, ushort length, CancellationToken ct)
    {
        if (_stream == null)
            return OperateResult.Fail<byte[]>(ErrorCode.CommNotConnected, "未连接");

        try
        {
            var addrInfo = ParseAddress(address);
            var request = BuildReadRequest(addrInfo, length);
            await _stream.WriteAsync(request, ct);

            var response = await ReadResponseAsync(ct);
            if (!response.IsSuccess)
                return OperateResult.Fail<byte[]>(response);

            // 解析响应数据
            var data = ParseReadResponse(response.Content!, length);
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
            return OperateResult.Fail(ErrorCode.CommNotConnected, "未连接");

        try
        {
            var addrInfo = ParseAddress(address);
            var request = BuildWriteRequest(addrInfo, data);
            await _stream.WriteAsync(request, ct);

            var response = await ReadResponseAsync(ct);
            if (!response.IsSuccess)
                return OperateResult.Fail(response);

            // 检查写入是否成功
            if (response.Content!.Length >= 17 && response.Content[13] == 0x00)
            {
                return OperateResult.Ok();
            }

            return OperateResult.Fail(ErrorCode.CommWriteFailed, "写入响应异常");
        }
        catch (Exception ex)
        {
            return OperateResult.Fail(ErrorCode.CommWriteFailed, ex.Message);
        }
    }

    /// <summary>
    /// 构建 3E 帧读请求。
    /// </summary>
    private byte[] BuildReadRequest(McAddressInfo addrInfo, ushort length)
    {
        lock (_tidLock)
        {
            var pdu = new List<byte>();

            // Subheader: 3E 帧 (binary)
            pdu.AddRange(new byte[] { 0x50, 0x00 });

            // Network number
            pdu.Add(0xFF);

            // PC number
            pdu.Add(0xFF);

            // Request destination module I/O number
            pdu.Add(0x03);
            pdu.Add(0x01);

            // Request destination module station number
            pdu.Add(0x00);

            // Request data length (will be fixed later)
            int dataLenPos = pdu.Count;
            pdu.AddRange(new byte[] { 0x00, 0x00 });

            // CPU monitoring timer
            pdu.Add(0x00);
            pdu.Add(0x00);

            // Command: Batch read
            pdu.AddRange(new byte[] { 0x01, 0x04 });

            // Sub command: Word access (0x0001) or bit access (0x0000)
            ushort subCommand = addrInfo.IsBit ? (ushort)0x0000 : (ushort)0x0001;
            pdu.Add((byte)(subCommand >> 8));
            pdu.Add((byte)subCommand);

            // Starting address (以字为单位)
            var startAddr = addrInfo.Address;
            pdu.Add((byte)((startAddr >> 24) & 0xFF));
            pdu.Add((byte)((startAddr >> 16) & 0xFF));
            pdu.Add((byte)((startAddr >> 8) & 0xFF));
            pdu.Add((byte)(startAddr & 0xFF));

            // Number of points (words for word access, bits for bit access)
            pdu.Add((byte)(length >> 8));
            pdu.Add((byte)length);

            // Fix data length
            int dataLen = pdu.Count - 11;
            pdu[dataLenPos] = (byte)(dataLen >> 8);
            pdu[dataLenPos + 1] = (byte)(dataLen & 0xFF);

            return pdu.ToArray();
        }
    }

    /// <summary>
    /// 构建 3E 帧写请求。
    /// </summary>
    private byte[] BuildWriteRequest(McAddressInfo addrInfo, byte[] data)
    {
        lock (_tidLock)
        {
            var pdu = new List<byte>();

            // Subheader: 3E 帧 (binary)
            pdu.AddRange(new byte[] { 0x50, 0x00 });

            // Network number
            pdu.Add(0xFF);

            // PC number
            pdu.Add(0xFF);

            // Request destination module I/O number
            pdu.Add(0x03);
            pdu.Add(0x01);

            // Request destination module station number
            pdu.Add(0x00);

            // Request data length (will be fixed later)
            int dataLenPos = pdu.Count;
            pdu.AddRange(new byte[] { 0x00, 0x00 });

            // CPU monitoring timer
            pdu.Add(0x00);
            pdu.Add(0x00);

            // Command: Batch write
            pdu.AddRange(new byte[] { 0x01, 0x14 });

            // Sub command: Word access (0x0001) or bit access (0x0000)
            ushort subCommand = addrInfo.IsBit ? (ushort)0x0000 : (ushort)0x0001;
            pdu.Add((byte)(subCommand >> 8));
            pdu.Add((byte)subCommand);

            // Starting address (以字为单位)
            var startAddr = addrInfo.Address;
            pdu.Add((byte)((startAddr >> 24) & 0xFF));
            pdu.Add((byte)((startAddr >> 16) & 0xFF));
            pdu.Add((byte)((startAddr >> 8) & 0xFF));
            pdu.Add((byte)(startAddr & 0xFF));

            // Number of points
            ushort points = (ushort)(data.Length / 2);
            pdu.Add((byte)(points >> 8));
            pdu.Add((byte)points);

            // Write data
            pdu.Add((byte)(data.Length >> 8));
            pdu.Add((byte)data.Length);
            pdu.AddRange(data);

            // Fix data length
            int dataLen = pdu.Count - 11;
            pdu[dataLenPos] = (byte)(dataLen >> 8);
            pdu[dataLenPos + 1] = (byte)(dataLen & 0xFF);

            _tid++;

            return pdu.ToArray();
        }
    }

    /// <summary>
    /// 读取响应。
    /// </summary>
    private async Task<OperateResult<byte[]>> ReadResponseAsync(CancellationToken ct)
    {
        var buffer = new byte[2048];
        var deadline = DateTime.Now.AddMilliseconds(Options.ReadTimeoutMs);
        var totalRead = 0;

        // 先读取头 11 字节
        while (totalRead < 11 && DateTime.Now < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (_stream!.DataAvailable)
            {
                var n = await _stream.ReadAsync(buffer.AsMemory(totalRead, 11 - totalRead), ct);
                if (n == 0) break;
                totalRead += n;
            }
            else
            {
                await Task.Delay(10, ct);
            }
        }

        if (totalRead < 11)
            return OperateResult.Fail<byte[]>(ErrorCode.CommTimeout, "响应头读取超时");

        // 获取数据长度
        var dataLen = (buffer[9] << 8) | buffer[10];
        var totalLen = 11 + dataLen;

        // 读取剩余数据
        while (totalRead < totalLen && DateTime.Now < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (_stream!.DataAvailable)
            {
                var n = await _stream.ReadAsync(buffer.AsMemory(totalRead, totalLen - totalRead), ct);
                if (n == 0) break;
                totalRead += n;
            }
            else
            {
                await Task.Delay(10, ct);
            }
        }

        if (totalRead < totalLen)
            return OperateResult.Fail<byte[]>(ErrorCode.CommTimeout, "响应数据读取超时");

        // 检查结束代码
        if (buffer[9] == 0 && buffer[10] >= 2)
        {
            var endCode = (buffer[11] << 8) | buffer[12];
            if (endCode != 0)
            {
                return OperateResult.Fail<byte[]>(ErrorCode.CommProtocolError,
                    $"MC 响应错误码: 0x{endCode:X4}");
            }
        }

        var result = new byte[totalRead];
        Array.Copy(buffer, result, totalRead);
        return OperateResult.Ok(result);
    }

    /// <summary>
    /// 解析读响应中的数据部分。
    /// </summary>
    private static byte[] ParseReadResponse(byte[] response, ushort expectedLength)
    {
        if (response.Length < 15)
            return Array.Empty<byte>();

        // 数据从第 14 字节开始（对于有数据的情况）
        // 响应格式: [0-1]Subheader [2]网络号 [3]PC号 [4-5]I/O [6]站号 [7-8]长度 [9-10]数据长 [11-12]结束码 [13...]数据
        int dataStart = 13;
        int dataLen = (response[9] << 8) | response[10] - 2; // 减去结束码

        if (response.Length < dataStart + dataLen)
            return Array.Empty<byte>();

        var data = new byte[dataLen];
        Array.Copy(response, dataStart, data, 0, dataLen);
        return data;
    }

    /// <summary>
    /// 解析三菱 MC 地址。
    /// 地址格式:
    /// - D0, D100 (数据寄存器)
    /// - X0, X1A0 (输入)
    /// - Y0, Y1A0 (输出)
    /// - M0, M1000 (辅助继电器)
    /// - L0 (链路继电器)
    /// - B0 (缓冲寄存器)
    /// - W0 (链接字软元件)
    /// - SM0 (特殊辅助)
    /// - SB0 (特殊链接)
    /// - SF0 (特殊文件)
    /// - Z0, ZR0 (文件寄存器)
    /// - TC0, TN0, TS0 (定时器)
    /// - CC0, CN0, CS0 (计数器)
    /// - S0 (步进继电器)
    /// </summary>
    private static McAddressInfo ParseAddress(string address)
    {
        var result = new McAddressInfo();

        if (string.IsNullOrWhiteSpace(address))
            return result;

        // 获取地址类型前缀
        var prefix = address.TakeWhile(char.IsLetter).ToArray();
        var typeStr = new string(prefix).ToUpperInvariant();
        var numPart = address.Substring(prefix.Length);

        // 解析地址数字（可能有子地址，如 D100.0 表示 bit）
        var dotIndex = numPart.IndexOf('.');
        bool isBit = false;
        int bitIndex = 0;

        if (dotIndex >= 0)
        {
            isBit = true;
            var numStr = numPart.Substring(0, dotIndex);
            if (int.TryParse(numPart.Substring(dotIndex + 1), out var bit))
            {
                bitIndex = bit;
            }
            numPart = numStr;
        }

        if (!int.TryParse(numPart, out var addrNum))
            addrNum = 0;

        result.IsBit = isBit;
        result.BitIndex = bitIndex;

        // 根据类型确定区域码
        switch (typeStr)
        {
            case "D":
            case "W":
                // D 和 W 是直接地址
                result.Area = 0xA8; // 0000 1010 1000 -> 对应 MC 协议的 D/W
                result.Address = (uint)addrNum;
                break;

            case "X":
                // X: 输入
                result.Area = 0x9C;
                result.Address = (uint)ParseOctal(addrNum);
                break;

            case "Y":
                // Y: 输出
                result.Area = 0x9D;
                result.Address = (uint)ParseOctal(addrNum);
                break;

            case "M":
                // M: 辅助继电器
                result.Area = 0x90;
                result.Address = (uint)addrNum;
                break;

            case "L":
                // L: 链路继电器
                result.Area = 0x92;
                result.Address = (uint)addrNum;
                break;

            case "B":
                // B: 缓冲寄存器
                result.Area = 0xA0;
                result.Address = (uint)addrNum;
                break;

            case "SM":
                // SM: 特殊辅助继电器
                result.Area = 0x91;
                result.Address = (uint)addrNum;
                break;

            case "SB":
                // SB: 特殊链接继电器
                result.Area = 0x93;
                result.Address = (uint)addrNum;
                break;

            case "SF":
                // SF: 特殊文件寄存器
                result.Area = 0x98;
                result.Address = (uint)addrNum;
                break;

            case "ZR":
                // ZR: 文件寄存器（连续）
                result.Area = 0xB0;
                result.Address = (uint)addrNum;
                break;

            case "Z":
                // Z: 索引寄存器
                result.Area = 0x9E;
                result.Address = (uint)addrNum;
                break;

            case "R":
                // R: 文件寄存器
                result.Area = 0xAF;
                result.Address = (uint)addrNum;
                break;

            case "W":
                // W: 链接字软元件
                result.Area = 0xB4;
                result.Address = (uint)addrNum;
                break;

            case "TC":
            case "TN":
            case "TS":
                // 定时器
                result.Area = 0xC0;
                result.Address = (uint)addrNum;
                break;

            case "CC":
            case "CN":
            case "CS":
                // 计数器
                result.Area = 0xC1;
                result.Address = (uint)addrNum;
                break;

            case "S":
                // 步进继电器
                result.Area = 0x98;
                result.Address = (uint)addrNum;
                break;

            default:
                // 默认为数据寄存器
                result.Area = 0xA8;
                result.Address = (uint)addrNum;
                break;
        }

        return result;
    }

    /// <summary>
    /// 解析八进制地址。
    /// </summary>
    private static int ParseOctal(int value)
    {
        // 三菱地址 X/Y 是八进制，需要转换
        return Convert.ToInt32(value.ToString(), 8);
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

    /// <summary>
    /// MC 地址信息。
    /// </summary>
    private struct McAddressInfo
    {
        public byte Area;
        public uint Address;
        public bool IsBit;
        public int BitIndex;
    }
}
