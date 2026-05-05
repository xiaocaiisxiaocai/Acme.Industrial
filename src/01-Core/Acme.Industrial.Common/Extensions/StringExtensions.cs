namespace Acme.Industrial.Common.Extensions;

/// <summary>
/// 字符串扩展方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 判断字符串是否为 null 或空
    /// </summary>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// 判断字符串是否为 null、空或仅包含空白
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// 如果字符串为 null 或空，返回默认值
    /// </summary>
    public static string OrDefault(this string? value, string defaultValue)
    {
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// 移除字符串末尾的指定字符（如果存在）
    /// </summary>
    public static string TrimEnd(this string value, char charToTrim)
    {
        while (value.EndsWith(charToTrim.ToString()))
        {
            value = value.Substring(0, value.Length - 1);
        }
        return value;
    }

    /// <summary>
    /// 安全转换为整数
    /// </summary>
    public static int? ToInt(this string value)
    {
        return int.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// 安全转换为双精度浮点数
    /// </summary>
    public static double? ToDouble(this string value)
    {
        return double.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// 将字符串转换为字节数组（ASCII 编码）
    /// </summary>
    public static byte[] ToAsciiBytes(this string value)
    {
        return System.Text.Encoding.ASCII.GetBytes(value);
    }

    /// <summary>
    /// 将字符串转换为字节数组（UTF-8 编码）
    /// </summary>
    public static byte[] ToUtf8Bytes(this string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// 格式化字符串，支持命名占位符
    /// </summary>
    public static string FormatWith(this string format, params (string Name, object Value)[] args)
    {
        foreach (var arg in args)
        {
            format = format.Replace($"{{{arg.Name}}}", arg.Value?.ToString() ?? string.Empty);
        }
        return format;
    }
}
