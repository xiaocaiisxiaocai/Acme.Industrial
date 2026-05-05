namespace Acme.Industrial.Common.Bytes;

/// <summary>
/// 字节数组转换核心类
/// </summary>
public class ByteTransform
{
    /// <summary>
    /// 数据解析格式
    /// </summary>
    public DataFormat DataFormat { get; set; } = DataFormat.ABCD;

    /// <summary>
    /// 是否倒转整个字节数组（影响多字节数据的字节序）
    /// </summary>
    public bool IsReverse { get; set; }

    /// <summary>
    /// 是否倒转字节对（Word Swap）
    /// </summary>
    public bool IsReverseWord { get; set; }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 Int16
    /// </summary>
    public short TransInt16(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[2];
        Array.Copy(buffer, startIndex, data, 0, 2);
        return BitConverter.ToInt16(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 UInt16
    /// </summary>
    public ushort TransUInt16(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[2];
        Array.Copy(buffer, startIndex, data, 0, 2);
        return BitConverter.ToUInt16(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 Int32
    /// </summary>
    public int TransInt32(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[4];
        Array.Copy(buffer, startIndex, data, 0, 4);
        return BitConverter.ToInt32(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 UInt32
    /// </summary>
    public uint TransUInt32(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[4];
        Array.Copy(buffer, startIndex, data, 0, 4);
        return BitConverter.ToUInt32(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 Int64
    /// </summary>
    public long TransInt64(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[8];
        Array.Copy(buffer, startIndex, data, 0, 8);
        return BitConverter.ToInt64(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 UInt64
    /// </summary>
    public ulong TransUInt64(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[8];
        Array.Copy(buffer, startIndex, data, 0, 8);
        return BitConverter.ToUInt64(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 Float
    /// </summary>
    public float TransFloat(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[4];
        Array.Copy(buffer, startIndex, data, 0, 4);
        return BitConverter.ToSingle(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将字节数组按照指定的格式转换为 Double
    /// </summary>
    public double TransDouble(byte[] buffer, int startIndex = 0)
    {
        var data = new byte[8];
        Array.Copy(buffer, startIndex, data, 0, 8);
        return BitConverter.ToDouble(GetEnergyFormat(data), 0);
    }

    /// <summary>
    /// 将 Int16 值转换为字节数组
    /// </summary>
    public byte[] TransByte(short value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 UInt16 值转换为字节数组
    /// </summary>
    public byte[] TransByte(ushort value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 Int32 值转换为字节数组
    /// </summary>
    public byte[] TransByte(int value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 UInt32 值转换为字节数组
    /// </summary>
    public byte[] TransByte(uint value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 Int64 值转换为字节数组
    /// </summary>
    public byte[] TransByte(long value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 UInt64 值转换为字节数组
    /// </summary>
    public byte[] TransByte(ulong value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 Float 值转换为字节数组
    /// </summary>
    public byte[] TransByte(float value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 将 Double 值转换为字节数组
    /// </summary>
    public byte[] TransByte(double value)
    {
        return GetEnergyFormat(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 根据当前的格式配置，获取处理后的字节数组
    /// </summary>
    private byte[] GetEnergyFormat(byte[] data)
    {
        if (data.Length == 1) return data;
        if (data.Length == 2) return ByByte2(data);
        if (data.Length == 4) return ByByte4(data);
        if (data.Length == 8) return ByByte8(data);
        return data;
    }

    /// <summary>
    /// 2字节数据的处理
    /// </summary>
    private byte[] ByByte2(byte[] data)
    {
        if (IsReverse) Array.Reverse(data);
        return data;
    }

    /// <summary>
    /// 4字节数据的处理
    /// </summary>
    private byte[] ByByte4(byte[] data)
    {
        switch (DataFormat)
        {
            case DataFormat.ABCD: break;
            case DataFormat.BADC: Array.Reverse(data); break;
            case DataFormat.CDAB: Array.Reverse(data, 0, 2); Array.Reverse(data, 2, 2); break;
            case DataFormat.DCBA: Array.Reverse(data); break;
        }
        if (IsReverse) Array.Reverse(data);
        return data;
    }

    /// <summary>
    /// 8字节数据的处理
    /// </summary>
    private byte[] ByByte8(byte[] data)
    {
        switch (DataFormat)
        {
            case DataFormat.ABCD: break;
            case DataFormat.BADC: Array.Reverse(data); break;
            case DataFormat.CDAB: Array.Reverse(data, 0, 2); Array.Reverse(data, 2, 2); Array.Reverse(data, 4, 2); Array.Reverse(data, 6, 2); break;
            case DataFormat.DCBA: Array.Reverse(data); break;
        }
        if (IsReverse) Array.Reverse(data);
        return data;
    }
}

/// <summary>
/// 数据解析格式枚举
/// </summary>
public enum DataFormat
{
    /// <summary>
    /// ABCD - Big Endian (高字节在前)
    /// </summary>
    ABCD,

    /// <summary>
    /// BADC - 大端字交换
    /// </summary>
    BADC,

    /// <summary>
    /// CDAB - 小端字交换
    /// </summary>
    CDAB,

    /// <summary>
    /// DCBA - Little Endian (低字节在前)
    /// </summary>
    DCBA
}
