namespace Acme.Industrial.Core.Configuration;

/// <summary>
/// 配置变更事件参数。
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    /// <summary>配置键。</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>旧值。</summary>
    public string? OldValue { get; init; }

    /// <summary>新值。</summary>
    public string? NewValue { get; init; }
}

/// <summary>
/// 应用程序配置接口。
/// </summary>
public interface IAppConfiguration
{
    /// <summary>
    /// 读取节点（如 "Database:ConnectionString"）。
    /// </summary>
    string? GetValue(string key);

    /// <summary>
    /// 读取节点（带默认值）。
    /// </summary>
    string GetValue(string key, string defaultValue);

    /// <summary>
    /// 读取强类型节点。
    /// </summary>
    T? GetSection<T>(string section) where T : class, new();

    /// <summary>
    /// 读取强类型节点（带默认值）。
    /// </summary>
    T GetSection<T>(string section, T defaultValue) where T : class, new();

    /// <summary>
    /// 设置值并保存。
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    /// 检查键是否存在。
    /// </summary>
    bool Contains(string key);

    /// <summary>
    /// 配置变更事件。
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? Changed;
}
