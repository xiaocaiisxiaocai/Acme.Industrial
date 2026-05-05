using System.Security.Cryptography;
using System.Text;

namespace Acme.Industrial.Common.Helpers;

/// <summary>
/// AES 加密解密工具类
/// </summary>
public static class AesHelper
{
    /// <summary>
    /// AES 加密
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="key">密钥（16/24/32字节）</param>
    /// <param name="iv">向量（16字节，可为空）</param>
    /// <returns>密文（Base64）</returns>
    public static string Encrypt(string plainText, byte[] key, byte[]? iv = null)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv ?? aes.IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// AES 解密
    /// </summary>
    /// <param name="cipherText">密文（Base64）</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">向量</param>
    /// <returns>明文</returns>
    public static string Decrypt(string cipherText, byte[] key, byte[]? iv = null)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentNullException(nameof(cipherText));

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv ?? aes.IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// 使用密码生成 AES 密钥
    /// </summary>
    /// <param name="password">密码</param>
    /// <param name="keySize">密钥大小（128/192/256）</param>
    /// <returns>密钥和IV元组</returns>
    public static (byte[] Key, byte[] IV) DeriveKeyFromPassword(string password, int keySize = 256)
    {
        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(keySize / 8);
        return (key, deriveBytes.GetBytes(16));
    }

    /// <summary>
    /// 生成随机密钥
    /// </summary>
    /// <param name="keySize">密钥大小（128/192/256）</param>
    /// <returns>随机密钥</returns>
    public static byte[] GenerateKey(int keySize = 256)
    {
        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return aes.Key;
    }

    /// <summary>
    /// 生成随机IV
    /// </summary>
    public static byte[] GenerateIV()
    {
        using var aes = Aes.Create();
        aes.GenerateIV();
        return aes.IV;
    }
}
