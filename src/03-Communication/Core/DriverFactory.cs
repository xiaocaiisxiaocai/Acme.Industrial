using Acme.Industrial.Core.Logging;
using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Core;

/// <summary>
/// 驱动工厂实现 - 管理协议驱动的注册和创建。
/// </summary>
public class DriverFactory : IDriverFactory
{
    private readonly ConcurrentDictionary<string, Func<ConnectionOptions, IDeviceDriver>> _factories = new();
    private readonly ConcurrentDictionary<string, string> _protocolDescriptions = new();
    private readonly IAppLogger _logger;
    private readonly IByteTransform _byteTransform;
    private readonly IAppLoggerFactory _loggerFactory;
    private bool _disposed;

    /// <summary>
    /// 构造函数。
    /// </summary>
    public DriverFactory(IByteTransform byteTransform, IAppLoggerFactory loggerFactory)
    {
        _byteTransform = byteTransform ?? throw new ArgumentNullException(nameof(byteTransform));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger(nameof(DriverFactory));
    }

    /// <summary>
    /// 注册协议驱动。
    /// </summary>
    public void Register(string protocol, Func<ConnectionOptions, IDeviceDriver> factory)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(protocol))
            throw new ArgumentException("协议名称不能为空", nameof(protocol));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        var normalizedProtocol = protocol.ToUpperInvariant();

        if (!_factories.TryAdd(normalizedProtocol, factory))
        {
            _logger.Warn($"协议 {protocol} 已注册，将被覆盖");
        }

        _logger.Info($"协议已注册: {protocol}");
    }

    /// <summary>
    /// 注册协议驱动（带描述）。
    /// </summary>
    public void Register(string protocol, string description, Func<ConnectionOptions, IDeviceDriver> factory)
    {
        Register(protocol, factory);
        _protocolDescriptions[protocol.ToUpperInvariant()] = description;
    }

    /// <summary>
    /// 创建驱动实例。
    /// </summary>
    public IDeviceDriver Create(ConnectionOptions options)
    {
        ThrowIfDisposed();

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.Protocol))
            throw new ArgumentException("协议不能为空", nameof(options));

        var normalizedProtocol = options.Protocol.ToUpperInvariant();

        if (!_factories.TryGetValue(normalizedProtocol, out var factory))
        {
            throw new NotSupportedException($"不支持的协议: {options.Protocol}。支持的协议: {string.Join(", ", GetSupportedProtocols())}");
        }

        // 选择字节转换器
        var byteTransform = SelectByteTransform(options.Endian);

        // 创建驱动实例
        var driver = factory(options);

        // 设置日志工厂（如果驱动支持）
        if (driver is DeviceDriverBase baseDriver)
        {
            // 字节转换器已在基类构造函数中设置
            _logger.Debug($"驱动已创建: {options.DeviceId} ({options.Protocol})");
        }

        return driver;
    }

    /// <summary>
    /// 获取支持的协议列表。
    /// </summary>
    public IReadOnlyList<string> GetSupportedProtocols()
    {
        ThrowIfDisposed();
        return _factories.Keys.OrderBy(k => k).ToList();
    }

    /// <summary>
    /// 检查协议是否支持。
    /// </summary>
    public bool IsProtocolSupported(string protocol)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(protocol)) return false;
        return _factories.ContainsKey(protocol.ToUpperInvariant());
    }

    /// <summary>
    /// 获取协议描述。
    /// </summary>
    public string? GetProtocolDescription(string protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol)) return null;
        return _protocolDescriptions.GetValueOrDefault(protocol.ToUpperInvariant());
    }

    /// <summary>
    /// 根据字节序选择字节转换器。
    /// </summary>
    private IByteTransform SelectByteTransform(EndianFormat endian)
    {
        return endian switch
        {
            EndianFormat.BigEndian => new BigEndianByteTransform(),
            EndianFormat.LittleEndian => new LittleEndianByteTransform(),
            EndianFormat.BigEndianSwap => new BigEndianSwapByteTransform(),
            EndianFormat.LittleEndianSwap => new LittleEndianSwapByteTransform(),
            _ => _byteTransform
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DriverFactory));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _factories.Clear();
        _protocolDescriptions.Clear();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 小端字节交换转换。
/// </summary>
public class LittleEndianSwapByteTransform : IByteTransform
{
    public EndianFormat Endian => EndianFormat.LittleEndianSwap;

    public bool ToBool(byte[] buffer, int index) => buffer[index] != 0;
    public short ToInt16(byte[] buffer, int index) => BitConverter.ToInt16(SwapBytes(buffer, index, 2), 0);
    public ushort ToUInt16(byte[] buffer, int index) => BitConverter.ToUInt16(SwapBytes(buffer, index, 2), 0);
    public int ToInt32(byte[] buffer, int index) => BitConverter.ToInt32(SwapBytes(buffer, index, 4), 0);
    public uint ToUInt32(byte[] buffer, int index) => BitConverter.ToUInt32(SwapBytes(buffer, index, 4), 0);
    public long ToInt64(byte[] buffer, int index) => BitConverter.ToInt64(SwapBytes(buffer, index, 8), 0);
    public ulong ToUInt64(byte[] buffer, int index) => BitConverter.ToUInt64(SwapBytes(buffer, index, 8), 0);
    public float ToFloat(byte[] buffer, int index) => BitConverter.ToSingle(SwapBytes(buffer, index, 4), 0);
    public double ToDouble(byte[] buffer, int index) => BitConverter.ToDouble(SwapBytes(buffer, index, 8), 0);

    public string ToString(byte[] buffer, int index, int length, System.Text.Encoding encoding)
        => encoding.GetString(buffer, index, length).TrimEnd('\0');

    public byte[] FromBool(bool value) => new[] { (byte)(value ? 1 : 0) };
    public byte[] FromInt16(short value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromUInt16(ushort value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromInt32(int value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromUInt32(uint value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromInt64(long value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromUInt64(ulong value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromFloat(float value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromDouble(double value) => SwapBytes(BitConverter.GetBytes(value));

    public byte[] FromString(string value, System.Text.Encoding encoding)
    {
        var bytes = encoding.GetBytes(value);
        var result = new byte[bytes.Length + 1];
        Array.Copy(bytes, result, bytes.Length);
        return result;
    }

    private static byte[] SwapBytes(byte[] bytes) => SwapBytes(bytes, 0, bytes.Length);
    private static byte[] SwapBytes(byte[] bytes, int index, int length)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i += 2)
        {
            result[i] = bytes[index + i + 1];
            result[i + 1] = bytes[index + i];
        }
        return result;
    }
}
