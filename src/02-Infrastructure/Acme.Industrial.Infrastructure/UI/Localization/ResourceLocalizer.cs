using System.Globalization;
using System.Resources;
using System.Text.Json;
using Acme.Industrial.Core.UI.Localization;
using Acme.Industrial.Core.Caching;

namespace Acme.Industrial.Infrastructure.UI.Localization;

/// <summary>
/// 基于资源文件的本地化服务实现。
/// </summary>
public class ResourceLocalizer : ILocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources;
    private string _currentCulture;
    private readonly string _defaultCulture;
    private List<string> _availableCulturesList;

    public event EventHandler? CultureChanged;

    public string CurrentCulture => _currentCulture;

    public ResourceLocalizer(string resourcesPath, string defaultCulture = "zh-CN")
    {
        _defaultCulture = defaultCulture;
        _currentCulture = defaultCulture;
        _resources = new Dictionary<string, Dictionary<string, string>>();
        _availableCulturesList = new List<string>();

        LoadResources(resourcesPath);
    }

    private IReadOnlyList<string> _availableCultures => _availableCulturesList;

    public string this[string key] => Get(key);

    public string Get(string key, params object[] args)
    {
        if (!_resources.TryGetValue(_currentCulture, out var culture) ||
            !culture.TryGetValue(key, out var value))
        {
            if (_currentCulture != _defaultCulture &&
                _resources.TryGetValue(_defaultCulture, out var defaultCulture) &&
                defaultCulture.TryGetValue(key, out var defaultValue))
            {
                value = defaultValue;
            }
            else
            {
                return key;
            }
        }

        return args.Length > 0 ? string.Format(value, args) : value;
    }

    public void ChangeCulture(string cultureName)
    {
        if (!_resources.ContainsKey(cultureName))
        {
            throw new CultureNotFoundException($"不支持的文化: {cultureName}");
        }

        _currentCulture = cultureName;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<string> GetAvailableCultures()
    {
        return _availableCultures;
    }

    private void LoadResources(string resourcesPath)
    {
        try
        {
            if (Directory.Exists(resourcesPath))
            {
                foreach (var file in Directory.GetFiles(resourcesPath, "*.json"))
                {
                    var cultureName = Path.GetFileNameWithoutExtension(file);
                    var content = File.ReadAllText(file);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(content);

                    if (dict != null)
                    {
                        _resources[cultureName] = dict;
                    }
                }
            }

            if (_resources.Count == 0)
            {
                _resources[_defaultCulture] = new Dictionary<string, string>
                {
                    ["App.Title"] = "工业监控系统",
                    ["App.Welcome"] = "欢迎使用工业监控系统",
                    ["Common.Save"] = "保存",
                    ["Common.Cancel"] = "取消",
                    ["Common.Delete"] = "删除",
                    ["Common.Edit"] = "编辑",
                    ["Common.Add"] = "新增",
                    ["Common.Search"] = "搜索",
                    ["Common.Refresh"] = "刷新",
                    ["Common.Export"] = "导出",
                    ["Common.Import"] = "导入",
                    ["Common.Confirm"] = "确认",
                    ["Common.Success"] = "操作成功",
                    ["Common.Error"] = "操作失败",
                    ["Common.Warning"] = "警告",
                    ["Common.Info"] = "提示",
                    ["Common.Loading"] = "加载中...",
                    ["Common.NoData"] = "暂无数据",
                    ["Common.ConfirmDelete"] = "确定要删除吗？",
                    ["Device.Online"] = "在线",
                    ["Device.Offline"] = "离线",
                    ["Device.Connected"] = "已连接",
                    ["Device.Disconnected"] = "已断开",
                    ["Alarm.Level"] = "报警等级",
                    ["Alarm.Time"] = "报警时间",
                    ["Alarm.Message"] = "报警信息",
                    ["Alarm.Acknowledge"] = "确认",
                    ["Alarm.Clear"] = "清除"
                };
            }

            _availableCulturesList = _resources.Keys.ToList();
        }
        catch (Exception)
        {
            _resources[_defaultCulture] = new Dictionary<string, string>();
            _availableCulturesList = new List<string> { _defaultCulture };
        }
    }
}

/// <summary>
/// 数据库支持的本地化服务（存储在数据库中）。
/// </summary>
public class DatabaseLocalizer : ILocalizer
{
    private readonly Func<Task<Dictionary<string, string>>> _loader;
    private Dictionary<string, string> _resources = new();
    private string _currentCulture;
    private readonly IReadOnlyList<string> _availableCultures;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public event EventHandler? CultureChanged;

    public string CurrentCulture => _currentCulture;

    public DatabaseLocalizer(
        Func<Task<Dictionary<string, string>>> loader,
        string defaultCulture = "zh-CN",
        IReadOnlyList<string>? availableCultures = null)
    {
        _loader = loader;
        _currentCulture = defaultCulture;
        _availableCultures = availableCultures ?? new List<string> { defaultCulture };
    }

    public string this[string key] => Get(key);

    public string Get(string key, params object[] args)
    {
        if (_resources.TryGetValue(key, out var value))
        {
            return args.Length > 0 ? string.Format(value, args) : value;
        }
        return key;
    }

    public async Task RefreshAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _resources = await _loader();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ChangeCulture(string cultureName)
    {
        if (!_availableCultures.Contains(cultureName))
        {
            throw new CultureNotFoundException($"不支持的文化: {cultureName}");
        }

        _currentCulture = cultureName;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<string> GetAvailableCultures()
    {
        return _availableCultures;
    }
}

/// <summary>
/// 内存缓存的本地化服务。
/// </summary>
public class CachedLocalizer : ILocalizer
{
    private readonly ILocalizer _innerLocalizer;
    private readonly IAppCache _cache;
    private readonly TimeSpan _cacheDuration;

    public event EventHandler? CultureChanged
    {
        add => _innerLocalizer.CultureChanged += value;
        remove => _innerLocalizer.CultureChanged -= value;
    }

    public string CurrentCulture => _innerLocalizer.CurrentCulture;

    public CachedLocalizer(ILocalizer innerLocalizer, IAppCache cache, TimeSpan? cacheDuration = null)
    {
        _innerLocalizer = innerLocalizer;
        _cache = cache;
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(30);
    }

    public string this[string key] => Get(key);

    public string Get(string key, params object[] args)
    {
        var cacheKey = $"localizer:{_innerLocalizer.CurrentCulture}:{key}";

        var cached = _cache.GetAsync<string>(cacheKey).GetAwaiter().GetResult();
        if (cached != null)
        {
            return args.Length > 0 ? string.Format(cached, args) : cached;
        }

        var value = _innerLocalizer.Get(key, args);
        _cache.SetAsync(cacheKey, value, _cacheDuration).GetAwaiter().GetResult();

        return value;
    }

    public void ChangeCulture(string cultureName)
    {
        _innerLocalizer.ChangeCulture(cultureName);
        _cache.ClearAsync().GetAwaiter().GetResult();
    }

    public IReadOnlyList<string> GetAvailableCultures()
    {
        return _innerLocalizer.GetAvailableCultures();
    }
}
