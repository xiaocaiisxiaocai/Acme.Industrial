namespace Acme.Industrial.Communication.Abstractions.Enumerations;

/// <summary>
/// 数据类型。
/// </summary>
public enum DataType
{
    Bool,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float,
    Double,
    String,
    ByteArray
}

/// <summary>
/// 字节序格式。
/// </summary>
public enum EndianFormat
{
    /// <summary>ABCD - 大端，高字节高字在前（Modbus 默认）。</summary>
    BigEndian,

    /// <summary>DCBA - 小端，低字节低字在前（x86 内存默认）。</summary>
    LittleEndian,

    /// <summary>BADC - 字节交换大端（西门子常见）。</summary>
    BigEndianSwap,

    /// <summary>CDAB - 字节交换小端（部分 PLC）。</summary>
    LittleEndianSwap
}

/// <summary>
/// 访问模式。
/// </summary>
public enum AccessMode
{
    ReadOnly,
    WriteOnly,
    ReadWrite
}

/// <summary>
/// 连接状态。
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Faulted
}

/// <summary>
/// 点位质量。
/// </summary>
public enum TagQuality
{
    Good,
    Bad,
    Uncertain
}
