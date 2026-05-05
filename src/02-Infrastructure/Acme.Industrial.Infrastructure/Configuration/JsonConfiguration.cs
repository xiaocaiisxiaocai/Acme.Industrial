using System.Text.Json;
using Acme.Industrial.Core.Configuration;

namespace Acme.Industrial.Infrastructure.Configuration;

/// <summary>
/// JSON 配置文件实现。
/// </summary>
public class JsonConfiguration : IAppConfiguration
{
    private readonly string _configPath;
    private Dictionary<string, object?> _config;
    private readonly FileSystemWatcher? _watcher;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IReadOnlyList<string> _searchPaths;

    public event EventHandler<ConfigChangedEventArgs>? Changed;

    public JsonConfiguration(string configPath, params string[] searchPaths)
    {
        _configPath = configPath;
        _searchPaths = searchPaths.ToList();
        _config = new Dictionary<string, object?>();

        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            _watcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(_configPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnConfigFileChanged;
        }
        else
        {
            _watcher = null;
        }

        LoadConfig();
    }

    public string? GetValue(string key)
    {
        return GetValueByPath(key)?.ToString();
    }

    public string GetValue(string key, string defaultValue)
    {
        var value = GetValue(key);
        return value ?? defaultValue;
    }

    public T? GetSection<T>(string section) where T : class, new()
    {
        return GetSection(section, new T());
    }

    public T GetSection<T>(string section, T defaultValue) where T : class, new()
    {
        var sectionData = GetValueByPath(section);
        if (sectionData == null)
        {
            return defaultValue;
        }

        try
        {
            if (sectionData is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? defaultValue;
            }
            if (sectionData is T typed)
            {
                return typed;
            }
            var json = JsonSerializer.Serialize(sectionData);
            return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        await _semaphore.WaitAsync();
        try
        {
            SetValueByPath(key, value);
            await SaveConfigAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public bool Contains(string key)
    {
        return GetValueByPath(key) != null;
    }

    private object? GetValueByPath(string path)
    {
        var parts = path.Split(':');
        object? current = _config;

        foreach (var part in parts)
        {
            if (current == null) return null;

            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else if (current is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        if (current is JsonElement elementStr && elementStr.ValueKind == JsonValueKind.String)
            return elementStr.GetString();
        if (current is JsonElement elementNum && elementNum.ValueKind == JsonValueKind.Number)
            return elementNum.ToString();
        if (current is JsonElement elementBool && elementBool.ValueKind == JsonValueKind.True)
            return "true";
        if (current is JsonElement elementFalse && elementFalse.ValueKind == JsonValueKind.False)
            return "false";

        return current;
    }

    private void SetValueByPath(string path, string value)
    {
        var parts = path.Split(':');
        var current = _config;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.ContainsKey(parts[i]))
            {
                current[parts[i]] = new Dictionary<string, object?>();
            }
            current = (current[parts[i]] as Dictionary<string, object?>)!;
        }

        var key = parts[^1];
        if (bool.TryParse(value, out var boolVal))
            current[key] = boolVal;
        else if (int.TryParse(value, out var intVal))
            current[key] = intVal;
        else if (double.TryParse(value, out var doubleVal))
            current[key] = doubleVal;
        else
            current[key] = value;
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var doc = JsonDocument.Parse(json);
                _config = JsonElementToDictionary(doc.RootElement);
            }
            else
            {
                foreach (var searchPath in _searchPaths)
                {
                    if (File.Exists(searchPath))
                    {
                        var json = File.ReadAllText(searchPath);
                        var doc = JsonDocument.Parse(json);
                        _config = JsonElementToDictionary(doc.RootElement);
                        break;
                    }
                }
            }
        }
        catch
        {
            _config = new Dictionary<string, object?>();
        }
    }

    private Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>();

        foreach (var prop in element.EnumerateObject())
        {
            result[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.Object => JsonElementToDictionary(prop.Value),
                JsonValueKind.Array => prop.Value.EnumerateArray()
                    .Select(e => (object?)e.ToString()).ToList(),
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }

        return result;
    }

    private async Task SaveConfigAsync()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_configPath, json);
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Task.Run(async () =>
        {
            await Task.Delay(100);
            await _semaphore.WaitAsync();
            try
            {
                var oldConfig = _config;
                LoadConfig();

                foreach (var key in _config.Keys)
                {
                    var oldValue = GetStringValue(oldConfig, key);
                    var newValue = GetValueByPath(key)?.ToString();

                    if (oldValue != newValue)
                    {
                        Changed?.Invoke(this, new ConfigChangedEventArgs
                        {
                            Key = key,
                            OldValue = oldValue,
                            NewValue = newValue
                        });
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    private string? GetStringValue(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }
}

/// <summary>
/// 环境变量配置源。
/// </summary>
public class EnvironmentConfiguration : IAppConfiguration
{
    private readonly string _prefix;

    public event EventHandler<ConfigChangedEventArgs>? Changed;

    public EnvironmentConfiguration(string prefix = "APPSETTING_")
    {
        _prefix = prefix;
    }

    public string? GetValue(string key)
    {
        var envKey = $"{_prefix}{key.Replace(":", "_")}";
        return Environment.GetEnvironmentVariable(envKey);
    }

    public string GetValue(string key, string defaultValue)
    {
        return GetValue(key) ?? defaultValue;
    }

    public T? GetSection<T>(string section) where T : class, new()
    {
        return GetSection(section, new T());
    }

    public T GetSection<T>(string section, T defaultValue) where T : class, new()
    {
        var prefix = $"{_prefix}{section.Replace(":", "_")}_";
        var result = new T();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            var envKey = $"{prefix}{prop.Name}";
            var value = Environment.GetEnvironmentVariable(envKey);

            if (value != null && prop.CanWrite)
            {
                try
                {
                    var converted = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(result, converted);
                }
                catch { }
            }
        }

        return result;
    }

    public Task SetAsync(string key, string value)
    {
        var envKey = $"{_prefix}{key.Replace(":", "_")}";
        Environment.SetEnvironmentVariable(envKey, value);
        return Task.CompletedTask;
    }

    public bool Contains(string key)
    {
        var envKey = $"{_prefix}{key.Replace(":", "_")}";
        return Environment.GetEnvironmentVariable(envKey) != null;
    }
}

/// <summary>
/// 配置条目（用于组合配置）。
/// </summary>
internal class ConfigurationEntry
{
    public int Priority { get; }
    public IAppConfiguration Config { get; }

    public ConfigurationEntry(int priority, IAppConfiguration config)
    {
        Priority = priority;
        Config = config;
    }
}

/// <summary>
/// 组合配置源（支持多个配置源，按优先级合并）。
/// </summary>
public class CompositeConfiguration : IAppConfiguration
{
    private readonly List<ConfigurationEntry> _configurations = new();

    public event EventHandler<ConfigChangedEventArgs>? Changed;

    public void AddConfiguration(IAppConfiguration config, int priority)
    {
        if (priority <= 0) priority = 0;

        _configurations.Add(new ConfigurationEntry(priority, config));
        _configurations.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        config.Changed += OnChildConfigChanged;
    }

    public string? GetValue(string key)
    {
        foreach (var entry in _configurations)
        {
            var value = entry.Config.GetValue(key);
            if (value != null)
                return value;
        }
        return null;
    }

    public string GetValue(string key, string defaultValue)
    {
        return GetValue(key) ?? defaultValue;
    }

    public T? GetSection<T>(string section) where T : class, new()
    {
        return GetSection(section, new T());
    }

    public T GetSection<T>(string section, T defaultValue) where T : class, new()
    {
        foreach (var entry in _configurations)
        {
            var value = entry.Config.GetSection(section, defaultValue);
            if (value != null)
                return value;
        }
        return defaultValue;
    }

    public async Task SetAsync(string key, string value)
    {
        if (_configurations.Count > 0)
        {
            await _configurations[0].Config.SetAsync(key, value);
        }
    }

    public bool Contains(string key)
    {
        return _configurations.Any(c => c.Config.Contains(key));
    }

    private void OnChildConfigChanged(object? sender, ConfigChangedEventArgs e)
    {
        Changed?.Invoke(this, e);
    }
}
