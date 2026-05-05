using System.Text;

namespace Acme.Industrial.Communication.Abstractions;

/// <summary>
/// 字节转换接口。
/// </summary>
public interface IByteTransform
{
    /// <summary>
    /// 字节序格式。
    /// </summary>
    EndianFormat Endian { get; }

    // 字节 -> 数值
    bool ToBool(byte[] buffer, int index);
    short ToInt16(byte[] buffer, int index);
    ushort ToUInt16(byte[] buffer, int index);
    int ToInt32(byte[] buffer, int index);
    uint ToUInt32(byte[] buffer, int index);
    long ToInt64(byte[] buffer, int index);
    ulong ToUInt64(byte[] buffer, int index);
    float ToFloat(byte[] buffer, int index);
    double ToDouble(byte[] buffer, int index);
    string ToString(byte[] buffer, int index, int length, Encoding encoding);

    // 数值 -> 字节
    byte[] FromBool(bool value);
    byte[] FromInt16(short value);
    byte[] FromUInt16(ushort value);
    byte[] FromInt32(int value);
    byte[] FromUInt32(uint value);
    byte[] FromInt64(long value);
    byte[] FromUInt64(ulong value);
    byte[] FromFloat(float value);
    byte[] FromDouble(double value);
    byte[] FromString(string value, Encoding encoding);
}

/// <summary>
/// 大端字节转换。
/// </summary>
public class BigEndianByteTransform : IByteTransform
{
    public EndianFormat Endian => EndianFormat.BigEndian;

    public bool ToBool(byte[] buffer, int index) => buffer[index] != 0;
    public short ToInt16(byte[] buffer, int index) => BitConverter.ToInt16(buffer, index);
    public ushort ToUInt16(byte[] buffer, int index) => BitConverter.ToUInt16(buffer, index);
    public int ToInt32(byte[] buffer, int index) => BitConverter.ToInt32(buffer, index);
    public uint ToUInt32(byte[] buffer, int index) => BitConverter.ToUInt32(buffer, index);
    public long ToInt64(byte[] buffer, int index) => BitConverter.ToInt64(buffer, index);
    public ulong ToUInt64(byte[] buffer, int index) => BitConverter.ToUInt64(buffer, index);
    public float ToFloat(byte[] buffer, int index) => BitConverter.ToSingle(buffer, index);
    public double ToDouble(byte[] buffer, int index) => BitConverter.ToDouble(buffer, index);
    public string ToString(byte[] buffer, int index, int length, Encoding encoding)
        => encoding.GetString(buffer, index, length).TrimEnd('\0');

    public byte[] FromBool(bool value) => new[] { (byte)(value ? 1 : 0) };
    public byte[] FromInt16(short value) => BitConverter.GetBytes(value);
    public byte[] FromUInt16(ushort value) => BitConverter.GetBytes(value);
    public byte[] FromInt32(int value) => BitConverter.GetBytes(value);
    public byte[] FromUInt32(uint value) => BitConverter.GetBytes(value);
    public byte[] FromInt64(long value) => BitConverter.GetBytes(value);
    public byte[] FromUInt64(ulong value) => BitConverter.GetBytes(value);
    public byte[] FromFloat(float value) => BitConverter.GetBytes(value);
    public byte[] FromDouble(double value) => BitConverter.GetBytes(value);
    public byte[] FromString(string value, Encoding encoding)
    {
        var bytes = encoding.GetBytes(value);
        var result = new byte[bytes.Length + 1];
        Array.Copy(bytes, result, bytes.Length);
        return result;
    }
}

/// <summary>
/// 小端字节转换。
/// </summary>
public class LittleEndianByteTransform : IByteTransform
{
    public EndianFormat Endian => EndianFormat.LittleEndian;

    public bool ToBool(byte[] buffer, int index) => buffer[index] != 0;
    public short ToInt16(byte[] buffer, int index) => BitConverter.ToInt16(buffer, index);
    public ushort ToUInt16(byte[] buffer, int index) => BitConverter.ToUInt16(buffer, index);
    public int ToInt32(byte[] buffer, int index) => BitConverter.ToInt32(buffer, index);
    public uint ToUInt32(byte[] buffer, int index) => BitConverter.ToUInt32(buffer, index);
    public long ToInt64(byte[] buffer, int index) => BitConverter.ToInt64(buffer, index);
    public ulong ToUInt64(byte[] buffer, int index) => BitConverter.ToUInt64(buffer, index);
    public float ToFloat(byte[] buffer, int index) => BitConverter.ToSingle(buffer, index);
    public double ToDouble(byte[] buffer, int index) => BitConverter.ToDouble(buffer, index);
    public string ToString(byte[] buffer, int index, int length, Encoding encoding)
        => encoding.GetString(buffer, index, length).TrimEnd('\0');

    public byte[] FromBool(bool value) => new[] { (byte)(value ? 1 : 0) };
    public byte[] FromInt16(short value) => BitConverter.GetBytes(value);
    public byte[] FromUInt16(ushort value) => BitConverter.GetBytes(value);
    public byte[] FromInt32(int value) => BitConverter.GetBytes(value);
    public byte[] FromUInt32(uint value) => BitConverter.GetBytes(value);
    public byte[] FromInt64(long value) => BitConverter.GetBytes(value);
    public byte[] FromUInt64(ulong value) => BitConverter.GetBytes(value);
    public byte[] FromFloat(float value) => BitConverter.GetBytes(value);
    public byte[] FromDouble(double value) => BitConverter.GetBytes(value);
    public byte[] FromString(string value, Encoding encoding)
    {
        var bytes = encoding.GetBytes(value);
        var result = new byte[bytes.Length + 1];
        Array.Copy(bytes, result, bytes.Length);
        return result;
    }
}

/// <summary>
/// 大端字节交换转换（西门子常见）。
/// </summary>
public class BigEndianSwapByteTransform : IByteTransform
{
    public EndianFormat Endian => EndianFormat.BigEndianSwap;

    public bool ToBool(byte[] buffer, int index) => buffer[index] != 0;
    public short ToInt16(byte[] buffer, int index) => Swap(BitConverter.ToInt16(buffer, index));
    public ushort ToUInt16(byte[] buffer, int index) => Swap(BitConverter.ToUInt16(buffer, index));
    public int ToInt32(byte[] buffer, int index) => SwapInt32(BitConverter.ToInt32(buffer, index));
    public uint ToUInt32(byte[] buffer, int index) => SwapUInt32(BitConverter.ToUInt32(buffer, index));
    public long ToInt64(byte[] buffer, int index) => SwapInt64(BitConverter.ToInt64(buffer, index));
    public ulong ToUInt64(byte[] buffer, int index) => SwapUInt64(BitConverter.ToUInt64(buffer, index));
    public float ToFloat(byte[] buffer, int index) => BitConverter.ToSingle(SwapBytes(buffer, index, 4), 0);
    public double ToDouble(byte[] buffer, int index) => BitConverter.ToDouble(SwapBytes(buffer, index, 8), 0);
    public string ToString(byte[] buffer, int index, int length, Encoding encoding)
        => encoding.GetString(buffer, index, length).TrimEnd('\0');

    public byte[] FromBool(bool value) => new[] { (byte)(value ? 1 : 0) };
    public byte[] FromInt16(short value) => BitConverter.GetBytes(Swap(value));
    public byte[] FromUInt16(ushort value) => BitConverter.GetBytes(Swap(value));
    public byte[] FromInt32(int value) => BitConverter.GetBytes(SwapInt32(value));
    public byte[] FromUInt32(uint value) => BitConverter.GetBytes(SwapUInt32(value));
    public byte[] FromInt64(long value) => BitConverter.GetBytes(SwapInt64(value));
    public byte[] FromUInt64(ulong value) => BitConverter.GetBytes(SwapUInt64(value));
    public byte[] FromFloat(float value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromDouble(double value) => SwapBytes(BitConverter.GetBytes(value));
    public byte[] FromString(string value, Encoding encoding)
    {
        var bytes = encoding.GetBytes(value);
        var result = new byte[bytes.Length + 1];
        Array.Copy(bytes, result, bytes.Length);
        return result;
    }

    private static short Swap(short value) => (short)((value >> 8) | ((value & 0xFF) << 8));
    private static ushort Swap(ushort value) => (ushort)((value >> 8) | ((value & 0xFF) << 8));
    private static int SwapInt32(int value) => ((value >> 24) & 0xFF) | ((value >> 8) & 0xFF00) | ((value & 0xFF00) << 8) | ((value & 0xFF) << 24);
    private static uint SwapUInt32(uint value) => (value >> 24) | ((value >> 8) & 0xFF00) | ((value & 0xFF00) << 8) | ((value & 0xFF) << 24);
    private static long SwapInt64(long value) => ((value >> 56) & 0xFF) | ((value >> 40) & 0xFF00) | ((value >> 24) & 0xFF0000) | ((value >> 8) & 0xFF000000) | ((value & 0xFF000000) << 8) | ((value & 0xFF0000) << 24) | ((value & 0xFF00) << 40) | ((value & 0xFF) << 56);
    private static ulong SwapUInt64(ulong value) => (value >> 56) | ((value >> 40) & 0xFF00) | ((value >> 24) & 0xFF0000) | ((value >> 8) & 0xFF000000) | ((value & 0xFF000000) << 8) | ((value & 0xFF0000) << 24) | ((value & 0xFF00) << 40) | ((value & 0xFF) << 56);
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
