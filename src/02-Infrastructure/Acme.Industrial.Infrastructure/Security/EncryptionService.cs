using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Infrastructure.Security;

/// <summary>
/// 哈希算法类型。
/// </summary>
public enum HashAlgorithmType
{
    MD5,
    SHA1,
    SHA256,
    SHA384,
    SHA512
}

/// <summary>
/// 加密服务接口。
/// </summary>
public interface IEncryptionService
{
    string Hash(string input, HashAlgorithmType algorithm = HashAlgorithmType.SHA256);
    string Encrypt(string plainText, string key);
    string Decrypt(string cipherText, string key);
    string GenerateSalt(int length = 32);
    string HashPassword(string password, string salt);
    bool VerifyPassword(string password, string hash, string salt);
    string GenerateToken(int length = 32);
}

/// <summary>
/// 加密服务实现。
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IAppLogger _logger;

    public EncryptionService(IAppLogger logger)
    {
        _logger = logger;
    }

    public string Hash(string input, HashAlgorithmType algorithm = HashAlgorithmType.SHA256)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = ComputeHash(bytes, algorithm);
        return Convert.ToHexString(hash);
    }

    public string Encrypt(string plainText, string key)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.Error($"加密失败", ex);
            throw;
        }
    }

    public string Decrypt(string cipherText, string key)
    {
        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);

            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.Error($"解密失败", ex);
            throw;
        }
    }

    public string GenerateSalt(int length = 32)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashPassword(string password, string salt)
    {
        var combined = password + salt;
        var hash = ComputeHash(Encoding.UTF8.GetBytes(combined), HashAlgorithmType.SHA512);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        var computedHash = HashPassword(password, salt);
        return SecureCompare(hash, computedHash);
    }

    public string GenerateToken(int length = 32)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private static byte[] ComputeHash(byte[] input, HashAlgorithmType algorithm)
    {
        HashAlgorithm hashAlgorithm = algorithm switch
        {
            HashAlgorithmType.MD5 => MD5.Create(),
            HashAlgorithmType.SHA1 => SHA1.Create(),
            HashAlgorithmType.SHA256 => SHA256.Create(),
            HashAlgorithmType.SHA384 => SHA384.Create(),
            HashAlgorithmType.SHA512 => SHA512.Create(),
            _ => SHA256.Create()
        };

        using (hashAlgorithm)
        {
            return hashAlgorithm.ComputeHash(input);
        }
    }

    private static byte[] DeriveKey(string password)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(32);
    }

    private static bool SecureCompare(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}

/// <summary>
/// 连接字符串加密服务。
/// </summary>
public class ConnectionStringEncryptor
{
    private readonly IEncryptionService _encryption;

    public ConnectionStringEncryptor(IEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public string Encrypt(string connectionString)
    {
        return _encryption.Encrypt(connectionString, GetMachineKey());
    }

    public string Decrypt(string encryptedConnectionString)
    {
        return _encryption.Decrypt(encryptedConnectionString, GetMachineKey());
    }

    private static string GetMachineKey()
    {
        var machineName = Environment.MachineName;
        var userName = Environment.UserName;
        var combined = $"{machineName}:{userName}:AcmeIndustrial";

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}
