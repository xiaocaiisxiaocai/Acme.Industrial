namespace Acme.Industrial.Common.Checksum;

/// <summary>
/// CRC16 校验算法实现
/// </summary>
public static class Crc16
{
    /// <summary>
    /// Modbus CRC16 算法
    /// </summary>
    public static ushort Modbus(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// CRC16-CCITT 算法
    /// </summary>
    public static ushort CCITT(ReadOnlySpan<byte> data, ushort initial = 0xFFFF)
    {
        ushort crc = initial;

        foreach (byte b in data)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ 0x1021);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// CRC16-XModem 算法
    /// </summary>
    public static ushort XModem(ReadOnlySpan<byte> data)
    {
        return CCITT(data, 0x0000);
    }

    /// <summary>
    /// CRC16-IBM (同 Modbus)
    /// </summary>
    public static ushort IBM(ReadOnlySpan<byte> data)
    {
        return Modbus(data);
    }
}
