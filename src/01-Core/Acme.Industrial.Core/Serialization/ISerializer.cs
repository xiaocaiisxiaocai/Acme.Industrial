namespace Acme.Industrial.Core.Serialization;

/// <summary>
/// 序列化器类型。
/// </summary>
public enum JsonSerializerType
{
    SystemTextJson,
    DataContract,
    Newtonsoft
}

/// <summary>
/// 序列化器接口。
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// 序列化对象为 JSON 字符串。
    /// </summary>
    string Serialize<T>(T value);

    /// <summary>
    /// 反序列化 JSON 字符串为对象。
    /// </summary>
    T? Deserialize<T>(string json);

    /// <summary>
    /// 序列化对象为字节数组。
    /// </summary>
    byte[] SerializeToBytes<T>(T value);

    /// <summary>
    /// 从字节数组反序列化对象。
    /// </summary>
    T? DeserializeFromBytes<T>(byte[] data);

    /// <summary>
    /// 异步序列化。
    /// </summary>
    Task<string> SerializeAsync<T>(T value, CancellationToken ct = default);

    /// <summary>
    /// 异步反序列化。
    /// </summary>
    Task<T?> DeserializeAsync<T>(string json, CancellationToken ct = default);
}

/// <summary>
/// 对象扩展方法。
/// </summary>
public static class ObjectExtensions
{
    public static string ToJson<T>(this T value, ISerializer? serializer = null)
    {
        if (serializer == null)
        {
            var systemTextJsonType = Type.GetType("Acme.Industrial.Infrastructure.Serialization.SystemTextJsonSerializer, Acme.Industrial.Infrastructure");
            if (systemTextJsonType != null)
            {
                serializer = (ISerializer?)Activator.CreateInstance(systemTextJsonType);
            }
            else
            {
                throw new InvalidOperationException("未提供序列化器，且无法找到默认实现。请确保已引用 Infrastructure 项目。");
            }
        }
        return serializer.Serialize(value);
    }

    public static T? FromJson<T>(this string json, ISerializer? serializer = null)
    {
        if (serializer == null)
        {
            var systemTextJsonType = Type.GetType("Acme.Industrial.Infrastructure.Serialization.SystemTextJsonSerializer, Acme.Industrial.Infrastructure");
            if (systemTextJsonType != null)
            {
                serializer = (ISerializer?)Activator.CreateInstance(systemTextJsonType);
            }
            else
            {
                throw new InvalidOperationException("未提供序列化器，且无法找到默认实现。请确保已引用 Infrastructure 项目。");
            }
        }
        return serializer.Deserialize<T>(json);
    }
}
