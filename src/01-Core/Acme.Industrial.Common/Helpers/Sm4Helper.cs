using System.Security.Cryptography;
using System.Text;

namespace Acme.Industrial.Common.Helpers;

/// <summary>
/// SM4 国密加密算法工具类
/// SM4 是中国国家密码标准（GB/T 32907-2016）
/// </summary>
public static class Sm4Helper
{
    // SM4 S-box
    private static readonly byte[] SBox = new byte[256]
    {
        0xD6, 0x90, 0xE9, 0xFE, 0xCC, 0xE1, 0x3D, 0xB7, 0x16, 0xB6, 0x14, 0xC2, 0x28, 0xFB, 0x2C, 0x05,
        0x2B, 0x67, 0x9A, 0x76, 0x2A, 0xBE, 0x04, 0xC3, 0xAA, 0x44, 0x13, 0x26, 0x49, 0x86, 0x06, 0x99,
        0x9C, 0x42, 0x50, 0xF4, 0x91, 0xEF, 0x98, 0x7A, 0x33, 0x54, 0x0B, 0x43, 0xED, 0xCF, 0xAC, 0x62,
        0xE4, 0xB3, 0x1C, 0xA9, 0xC9, 0x08, 0xE8, 0x95, 0x80, 0xDF, 0x94, 0xFA, 0x75, 0x8F, 0x3F, 0xA6,
        0x47, 0x07, 0xA7, 0xFC, 0xF3, 0x73, 0x17, 0xBA, 0x83, 0x59, 0x3C, 0x19, 0xE6, 0x85, 0x4F, 0xA8,
        0x68, 0x6B, 0x81, 0xB2, 0x71, 0x64, 0xDA, 0x8B, 0xF8, 0xEB, 0x0F, 0x4B, 0x70, 0x56, 0x9D, 0x35,
        0x1E, 0x24, 0x0E, 0x5E, 0x63, 0x58, 0xD1, 0xA2, 0x25, 0x22, 0x7C, 0x3B, 0x01, 0x21, 0x78, 0x87,
        0xD4, 0x00, 0x46, 0x57, 0x9F, 0xD3, 0x27, 0x52, 0x4C, 0x36, 0x02, 0xE7, 0xA0, 0xC4, 0xC8, 0x9E,
        0xEA, 0xBF, 0x8A, 0xD2, 0x40, 0xC7, 0x38, 0xB5, 0xA3, 0xF7, 0xF2, 0xCE, 0xF9, 0x61, 0x15, 0xA1,
        0xE0, 0xAE, 0x5D, 0xA4, 0x9B, 0x34, 0x1A, 0x55, 0xAD, 0x93, 0x32, 0x30, 0xF5, 0x8C, 0xB1, 0xE3,
        0x1D, 0xF6, 0xE2, 0x2E, 0x82, 0x66, 0xCA, 0x60, 0xC0, 0x29, 0x23, 0xAB, 0x0D, 0x53, 0x4E, 0x6F,
        0xD5, 0xDB, 0x37, 0x45, 0xDE, 0xFD, 0x8E, 0x2F, 0x03, 0xFF, 0x6A, 0x72, 0x6D, 0x6C, 0x5B, 0x51,
        0x8D, 0x1B, 0xAF, 0x92, 0xBB, 0xDD, 0xBC, 0x7F, 0x11, 0xD9, 0x5C, 0x41, 0x1F, 0x10, 0x5A, 0xD8,
        0x0A, 0xC1, 0x31, 0x88, 0x51, 0x90, 0x19, 0xED, 0xE5, 0xF0, 0x38, 0xB8, 0xB4, 0x7B, 0x95, 0xA5,
        0x12, 0x93, 0xAB, 0x65, 0x98, 0x79, 0xFB, 0xF6, 0x7D, 0x96, 0x5F, 0x70, 0x08, 0x18, 0xD7, 0x4D,
        0xAE, 0x2D, 0x0C, 0xB9, 0xC6, 0x20, 0x68, 0x09, 0x97, 0x77, 0xB0, 0xAF, 0xBB, 0x16, 0x03, 0x86
    };

    // 系统参数
    private static readonly uint[] FK = { 0xA3B1BAC6, 0x56AA3350, 0x677D9197, 0xB27022DC };

    // 固定参数
    private static readonly uint[] CK = new uint[32]
    {
        0x00070E15, 0x1C232A31, 0x383F464D, 0x545B6269,
        0x70777E85, 0x8C939AA1, 0xA8AFB6BD, 0xC4CBD2D9,
        0xE0E7EEF5, 0xFC030A11, 0x181F262D, 0x343B4249,
        0x50575E65, 0x6C737A81, 0x888F969D, 0xA4ABB2B9,
        0xC0C7CED5, 0xDCE3EAF1, 0xF8FF060D, 0x141B2229,
        0x30373E45, 0x4C535A61, 0x686F767D, 0x848B9299,
        0xA0A7AEB5, 0xBCC3CAD1, 0xD8DFE6ED, 0xF4FB0209,
        0x10171E25, 0x2C333A41, 0x484F565D, 0x646B7279
    };

    /// <summary>
    /// SM4 加密（ECB模式）
    /// </summary>
    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        if (key.Length != 16)
            throw new ArgumentException("SM4 密钥必须为 16 字节", nameof(key));

        var expandedKey = KeyExpansion(key);
        var blocks = new List<byte[]>();

        for (int i = 0; i < plaintext.Length; i += 16)
        {
            var block = new byte[16];
            Array.Copy(plaintext, i, block, 0, Math.Min(16, plaintext.Length - i));
            if (block.Length < 16)
                Array.Resize(ref block, 16);
            blocks.Add(CryptRound(block, expandedKey, false));
        }

        var result = new byte[blocks.Count * 16];
        for (int i = 0; i < blocks.Count; i++)
            Array.Copy(blocks[i], 0, result, i * 16, 16);
        return result;
    }

    /// <summary>
    /// SM4 解密（ECB模式）
    /// </summary>
    public static byte[] Decrypt(byte[] ciphertext, byte[] key)
    {
        if (key.Length != 16)
            throw new ArgumentException("SM4 密钥必须为 16 字节", nameof(key));

        var expandedKey = KeyExpansion(key);
        var blocks = new List<byte[]>();

        for (int i = 0; i < ciphertext.Length; i += 16)
        {
            var block = new byte[16];
            Array.Copy(ciphertext, i, block, 0, 16);
            blocks.Add(CryptRound(block, expandedKey, true));
        }

        var result = new byte[blocks.Count * 16];
        for (int i = 0; i < blocks.Count; i++)
            Array.Copy(blocks[i], 0, result, i * 16, 16);
        return result;
    }

    /// <summary>
    /// SM4 加密字符串
    /// </summary>
    public static string Encrypt(string plaintext, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16, '0').Substring(0, 16));
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = Encrypt(plainBytes, keyBytes);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// SM4 解密字符串
    /// </summary>
    public static string Decrypt(string ciphertext, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16, '0').Substring(0, 16));
        var cipherBytes = Convert.FromBase64String(ciphertext);
        var decrypted = Decrypt(cipherBytes, keyBytes);
        return Encoding.UTF8.GetString(decrypted).TrimEnd('\0');
    }

    /// <summary>
    /// 生成随机密钥
    /// </summary>
    public static byte[] GenerateKey()
    {
        var key = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return key;
    }

    private static uint[] KeyExpansion(byte[] key)
    {
        var k = new uint[36];
        k[0] = BitConverter.ToUInt32(key, 0) ^ FK[0];
        k[1] = BitConverter.ToUInt32(key, 4) ^ FK[1];
        k[2] = BitConverter.ToUInt32(key, 8) ^ FK[2];
        k[3] = BitConverter.ToUInt32(key, 12) ^ FK[3];

        for (int i = 0; i < 32; i++)
        {
            k[i + 4] = k[i] ^ T(k[i + 1] ^ k[i + 2] ^ k[i + 3] ^ CK[i]);
        }

        return k;
    }

    private static byte[] CryptRound(byte[] input, uint[] key, bool decrypt)
    {
        var x = new uint[36];
        x[0] = BitConverter.ToUInt32(input, 0);
        x[1] = BitConverter.ToUInt32(input, 4);
        x[2] = BitConverter.ToUInt32(input, 8);
        x[3] = BitConverter.ToUInt32(input, 12);

        if (decrypt)
        {
            for (int i = 0; i < 32; i++)
            {
                x[i + 4] = x[i] ^ T(x[i + 1] ^ x[i + 2] ^ x[i + 3] ^ key[35 - i]);
            }
        }
        else
        {
            for (int i = 0; i < 32; i++)
            {
                x[i + 4] = x[i] ^ T(x[i + 1] ^ x[i + 2] ^ x[i + 3] ^ key[i + 4]);
            }
        }

        var output = new byte[16];
        Array.Copy(BitConverter.GetBytes(x[35]), 0, output, 0, 4);
        Array.Copy(BitConverter.GetBytes(x[34]), 0, output, 4, 4);
        Array.Copy(BitConverter.GetBytes(x[33]), 0, output, 8, 4);
        Array.Copy(BitConverter.GetBytes(x[32]), 0, output, 12, 4);
        return output;
    }

    private static uint T(uint input)
    {
        var sb = SBox[(input >> 24) & 0xFF];
        sb ^= SBox[(input >> 16) & 0xFF];
        sb ^= SBox[(input >> 8) & 0xFF];
        sb ^= SBox[input & 0xFF];
        return L(sb);
    }

    private static uint L(uint input)
    {
        return input ^ RotateLeft(input, 2) ^ RotateLeft(input, 10) ^ RotateLeft(input, 18) ^ RotateLeft(input, 24);
    }

    private static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }
}
