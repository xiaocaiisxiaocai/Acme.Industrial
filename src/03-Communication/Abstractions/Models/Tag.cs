using Acme.Industrial.Communication.Abstractions.Enumerations;

namespace Acme.Industrial.Communication.Abstractions.Models;

/// <summary>
/// 点位定义。
/// </summary>
public class Tag
{
    /// <summary>
    /// 点位唯一名称，如 "Reactor1.Temperature"。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 设备/PLC 内部地址，如 "40001"、"DB1.DBD0"、"D100"。
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// 数据类型。
    /// </summary>
    public DataType DataType { get; init; } = DataType.Int16;

    /// <summary>
    /// 字符串/字节数组的长度。
    /// </summary>
    public int Length { get; init; } = 1;

    /// <summary>
    /// 访问模式。
    /// </summary>
    public AccessMode AccessMode { get; init; } = AccessMode.ReadWrite;

    /// <summary>
    /// 采集周期（毫秒），仅订阅模式使用。
    /// </summary>
    public int ScanRate { get; init; } = 1000;

    /// <summary>
    /// 线性变换：value = raw * Scale + Offset。
    /// </summary>
    public double Scale { get; init; } = 1.0;

    /// <summary>
    /// 线性变换偏移量。
    /// </summary>
    public double Offset { get; init; } = 0.0;

    /// <summary>
    /// 工程单位，如 "℃"、"bar"。
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// 死区：变化超过此值才上报（绝对值）。
    /// </summary>
    public double DeadBand { get; init; } = 0.0;

    /// <summary>
    /// 描述。
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// 点位实时值。
/// </summary>
public class TagValue
{
    /// <summary>
    /// 点位名称。
    /// </summary>
    public string TagName { get; init; } = string.Empty;

    /// <summary>
    /// 值。
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// 质量。
    /// </summary>
    public TagQuality Quality { get; init; } = TagQuality.Bad;

    /// <summary>
    /// 时间戳。
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// 原始字节（调试用）。
    /// </summary>
    public byte[]? RawBytes { get; init; }
}
