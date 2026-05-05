namespace Acme.Industrial.Common.Checksum;

/// <summary>
/// LRC（纵向冗余校验）算法实现
/// 常用于串口通讯和 Modbus ASCII 模式
/// </summary>
public static class Lrc
{
    /// <summary>
    /// 计算 LRC 校验值
    /// </summary>
    /// <param name="data">输入数据</param>
    /// <returns>LRC 字节值</returns>
    public static byte Calculate(ReadOnlySpan<byte> data)
    {
        byte lrc = 0;
        foreach (byte b in data)
        {
            lrc = (byte)((lrc + b) & 0xFF);
        }
        return (byte)((~lrc) + 1);
    }

    /// <summary>
    /// 计算 LRC 校验值（从十六进制字符串）
    /// </summary>
    /// <param name="hexString">十六进制字符串（如 "01 03 00 00 00 0A"）</param>
    /// <returns>LRC 字节值</returns>
    public static byte CalculateFromHex(string hexString)
    {
        var bytes = new List<byte>();
        var parts = hexString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            bytes.Add(Convert.ToByte(part, 16));
        }
        return Calculate(bytes.ToArray());
    }

    /// <summary>
    /// 计算 LRC 并返回十六进制字符串
    /// </summary>
    public static string CalculateHex(ReadOnlySpan<byte> data)
    {
        return Calculate(data).ToString("X2");
    }
}
