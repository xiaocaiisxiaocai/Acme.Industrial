using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Industrial.Core.Serialization;

namespace Acme.Industrial.Infrastructure.Serialization;

/// <summary>
/// JSON 序列化选项配置。
/// </summary>
public class JsonSerializerOptions
{
    public bool WriteIndented { get; set; } = false;
    public bool IgnoreNullValues { get; set; } = false;
    public bool IgnoreReadOnlyProperties { get; set; } = false;
    public NamingConvention NamingConvention { get; set; } = NamingConvention.CamelCase;
    public JsonSerializerType Type { get; set; } = JsonSerializerType.SystemTextJson;
}

/// <summary>
/// 命名规范。
/// </summary>
public enum NamingConvention
{
    CamelCase,
    PascalCase,
    SnakeCase,
    KebabCase
}

/// <summary>
/// System.Text.Json 序列化器实现。
/// </summary>
public class SystemTextJsonSerializer : ISerializer
{
    private readonly System.Text.Json.JsonSerializerOptions _options;

    public SystemTextJsonSerializer(Action<JsonSerializerOptions>? configure = null)
    {
        var options = new JsonSerializerOptions();
        configure?.Invoke(options);

        _options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = options.WriteIndented,
            DefaultIgnoreCondition = options.IgnoreNullValues
                ? JsonIgnoreCondition.WhenWritingNull
                : JsonIgnoreCondition.Never,
            PropertyNamingPolicy = options.NamingConvention switch
            {
                NamingConvention.CamelCase => System.Text.Json.JsonNamingPolicy.CamelCase,
                _ => null
            },
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public string Serialize<T>(T value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value, _options);
    }

    public T? Deserialize<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
    }

    public byte[] SerializeToBytes<T>(T value)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, _options);
    }

    public T? DeserializeFromBytes<T>(byte[] data)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(data, _options);
    }

    public Task<string> SerializeAsync<T>(T value, CancellationToken ct = default)
    {
        return Task.FromResult(Serialize(value));
    }

    public async Task<T?> DeserializeAsync<T>(string json, CancellationToken ct = default)
    {
        return await Task.Run(() => Deserialize<T>(json), ct);
    }
}

/// <summary>
/// 数据合同序列化器（兼容 WCF）。
/// </summary>
public class DataContractJsonSerializer : ISerializer
{
    private readonly System.Runtime.Serialization.Json.DataContractJsonSerializerSettings _settings;

    public DataContractJsonSerializer(Type? knownTypes = null)
    {
        _settings = new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings
        {
            KnownTypes = knownTypes != null ? new[] { knownTypes } : Array.Empty<Type>(),
            DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("yyyy-MM-dd HH:mm:ss")
        };
    }

    public string Serialize<T>(T value)
    {
        using var stream = new MemoryStream();
        var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T), _settings);
        serializer.WriteObject(stream, value);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    public T? Deserialize<T>(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream(bytes);
        var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T), _settings);
        return (T?)serializer.ReadObject(stream);
    }

    public byte[] SerializeToBytes<T>(T value)
    {
        return System.Text.Encoding.UTF8.GetBytes(Serialize(value));
    }

    public T? DeserializeFromBytes<T>(byte[] data)
    {
        return Deserialize<T>(System.Text.Encoding.UTF8.GetString(data));
    }

    public Task<string> SerializeAsync<T>(T value, CancellationToken ct = default)
    {
        return Task.FromResult(Serialize(value));
    }

    public Task<T?> DeserializeAsync<T>(string json, CancellationToken ct = default)
    {
        return Task.FromResult(Deserialize<T>(json));
    }
}

/// <summary>
/// 压缩序列化器（使用 GZip 压缩）。
/// </summary>
public class CompressedJsonSerializer : ISerializer
{
    private readonly ISerializer _innerSerializer;

    public CompressedJsonSerializer(ISerializer innerSerializer)
    {
        _innerSerializer = innerSerializer;
    }

    public string Serialize<T>(T value)
    {
        var bytes = SerializeToBytes(value);
        return Convert.ToBase64String(Compress(bytes));
    }

    public T? Deserialize<T>(string json)
    {
        var bytes = Convert.FromBase64String(json);
        var decompressed = Decompress(bytes);
        return _innerSerializer.DeserializeFromBytes<T>(decompressed);
    }

    public byte[] SerializeToBytes<T>(T value)
    {
        return _innerSerializer.SerializeToBytes(value);
    }

    public T? DeserializeFromBytes<T>(byte[] data)
    {
        var decompressed = Decompress(data);
        return _innerSerializer.DeserializeFromBytes<T>(decompressed);
    }

    public Task<string> SerializeAsync<T>(T value, CancellationToken ct = default)
    {
        return Task.FromResult(Serialize(value));
    }

    public Task<T?> DeserializeAsync<T>(string json, CancellationToken ct = default)
    {
        return Task.FromResult(Deserialize<T>(json));
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}

/// <summary>
/// 序列化器工厂。
/// </summary>
public static class SerializerFactory
{
    public static ISerializer CreateJsonSerializer(Action<JsonSerializerOptions>? configure = null)
    {
        return new SystemTextJsonSerializer(configure);
    }

    public static ISerializer CreateCompressedJsonSerializer(Action<JsonSerializerOptions>? configure = null)
    {
        return new CompressedJsonSerializer(new SystemTextJsonSerializer(configure));
    }

    public static ISerializer CreateDataContractSerializer(Type? knownTypes = null)
    {
        return new DataContractJsonSerializer(knownTypes);
    }
}
