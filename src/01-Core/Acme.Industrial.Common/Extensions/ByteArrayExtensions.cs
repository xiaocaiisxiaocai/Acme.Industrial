namespace Acme.Industrial.Common.Extensions;

/// <summary>
/// 字节数组扩展方法
/// </summary>
public static class ByteArrayExtensions
{
    /// <summary>
    /// 将字节数组转换为十六进制字符串
    /// </summary>
    public static string ToHexString(this byte[] buffer, string separator = " ")
    {
        return BitConverter.ToString(buffer).Replace("-", separator);
    }

    /// <summary>
    /// 将十六进制字符串转换为字节数组
    /// </summary>
    public static byte[] FromHexString(this string hex)
    {
        hex = hex.Replace(" ", "").Replace("-", "");
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have even length");

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// 将字节数组转换为 Base64 字符串
    /// </summary>
    public static string ToBase64(this byte[] buffer)
    {
        return Convert.ToBase64String(buffer);
    }

    /// <summary>
    /// 将字节数组按指定长度分割
    /// </summary>
    public static IEnumerable<byte[]> Split(this byte[] buffer, int chunkSize)
    {
        for (int i = 0; i < buffer.Length; i += chunkSize)
        {
            int length = Math.Min(chunkSize, buffer.Length - i);
            var chunk = new byte[length];
            Array.Copy(buffer, i, chunk, 0, length);
            yield return chunk;
        }
    }

    /// <summary>
    /// 将两个字节数组合并
    /// </summary>
    public static byte[] Concat(this byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }

    /// <summary>
    /// 反转字节数组
    /// </summary>
    public static byte[] ReverseBytes(this byte[] buffer)
    {
        var result = new byte[buffer.Length];
        Array.Copy(buffer, result, buffer.Length);
        Array.Reverse(result);
        return result;
    }
}
