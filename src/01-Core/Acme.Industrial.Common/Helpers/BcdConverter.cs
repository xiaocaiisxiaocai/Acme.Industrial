namespace Acme.Industrial.Common.Helpers;

/// <summary>
/// BCD（Binary-Coded Decimal）码转换工具
/// 用于 PLC 与工业设备之间的数字显示
/// </summary>
public static class BcdConverter
{
    /// <summary>
    /// 将字节转换为 BCD 码
    /// </summary>
    public static byte ToBcd(byte value)
    {
        if (value > 99)
            throw new ArgumentOutOfRangeException(nameof(value), "值必须在 0-99 之间");

        return (byte)((value / 10) << 4 | value % 10);
    }

    /// <summary>
    /// 将 BCD 码转换为字节
    /// </summary>
    public static byte FromBcd(byte bcd)
    {
        return (byte)(((bcd >> 4) * 10) + (bcd & 0x0F));
    }

    /// <summary>
    /// 将整数转换为 BCD 字节数组
    /// </summary>
    /// <param name="value">整数值</param>
    /// <param name="length">BCD数组长度</param>
    public static byte[] IntToBcd(int value, int length)
    {
        var result = new byte[length];
        var isNegative = value < 0;
        var absValue = Math.Abs(value);

        for (int i = length - 1; i >= 0; i--)
        {
            result[i] = ToBcd((byte)(absValue % 100));
            absValue /= 100;
        }

        if (isNegative && length > 0)
        {
            result[0] |= 0x80; // 负数标记
        }

        return result;
    }

    /// <summary>
    /// 将 BCD 字节数组转换为整数
    /// </summary>
    public static int BcdToInt(byte[] bcd)
    {
        if (bcd == null || bcd.Length == 0)
            return 0;

        var result = 0;
        var multiplier = 1;
        var isNegative = (bcd[0] & 0x80) != 0;

        for (int i = bcd.Length - 1; i >= 0; i--)
        {
            result += FromBcd((byte)(bcd[i] & 0x7F)) * multiplier;
            multiplier *= 100;
        }

        return isNegative ? -result : result;
    }

    /// <summary>
    /// 将浮点数转换为 BCD 格式
    /// </summary>
    /// <param name="value">浮点值</param>
    /// <param name="integerDigits">整数部分位数</param>
    /// <param name="decimalDigits">小数部分位数</param>
    public static byte[] FloatToBcd(float value, int integerDigits, int decimalDigits)
    {
        var multiplier = (int)Math.Pow(10, decimalDigits);
        var scaled = (int)(value * multiplier + 0.5f); // 四舍五入
        return IntToBcd(scaled, integerDigits + decimalDigits);
    }

    /// <summary>
    /// 将 BCD 格式转换为浮点数
    /// </summary>
    public static float BcdToFloat(byte[] bcd, int decimalDigits)
    {
        var integer = BcdToInt(bcd);
        return integer / (float)Math.Pow(10, decimalDigits);
    }

    /// <summary>
    /// 验证是否为有效的 BCD 码
    /// </summary>
    public static bool IsValidBcd(byte bcd)
    {
        return ((bcd >> 4) <= 9) && ((bcd & 0x0F) <= 9);
    }

    /// <summary>
    /// 验证 BCD 数组是否全部有效
    /// </summary>
    public static bool IsValidBcdArray(byte[] data)
    {
        return data.All(IsValidBcd);
    }
}
