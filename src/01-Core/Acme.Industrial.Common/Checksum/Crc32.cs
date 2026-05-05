namespace Acme.Industrial.Common.Checksum;

/// <summary>
/// CRC32 校验算法实现
/// </summary>
public static class Crc32
{
    private static readonly uint[] Table;

    static Crc32()
    {
        Table = new uint[256];
        const uint polynomial = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            Table[i] = crc;
        }
    }

    /// <summary>
    /// 计算 CRC32 校验值
    /// </summary>
    public static uint Calculate(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte b in data)
        {
            byte index = (byte)((crc & 0xFF) ^ b);
            crc = (crc >> 8) ^ Table[index];
        }

        return ~crc;
    }

    /// <summary>
    /// 计算 CRC32 校验值，返回字节数组（小端序）
    /// </summary>
    public static byte[] CalculateBytes(ReadOnlySpan<byte> data)
    {
        var value = Calculate(data);
        return new byte[]
        {
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 24) & 0xFF)
        };
    }
}
