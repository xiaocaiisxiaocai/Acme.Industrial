namespace Acme.Industrial.Core.UI.Localization;

/// <summary>
/// 本地化器接口。
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// 获取本地化字符串。
    /// </summary>
    string this[string key] { get; }

    /// <summary>
    /// 获取本地化字符串。
    /// </summary>
    string Get(string key, params object[] args);

    /// <summary>
    /// 当前文化。
    /// </summary>
    string CurrentCulture { get; }

    /// <summary>
    /// 更改文化。
    /// </summary>
    void ChangeCulture(string cultureName);

    /// <summary>
    /// 文化变更事件。
    /// </summary>
    event EventHandler? CultureChanged;

    /// <summary>
    /// 获取所有可用文化。
    /// </summary>
    IReadOnlyList<string> GetAvailableCultures();
}
