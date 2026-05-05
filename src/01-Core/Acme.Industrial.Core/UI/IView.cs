namespace Acme.Industrial.Core.UI;

/// <summary>
/// 视图接口。
/// </summary>
public interface IView
{
    /// <summary>
    /// 显示加载中。
    /// </summary>
    void ShowLoading(string? message = null);

    /// <summary>
    /// 隐藏加载。
    /// </summary>
    void HideLoading();

    /// <summary>
    /// 显示消息。
    /// </summary>
    void ShowMessage(string message);

    /// <summary>
    /// 显示错误。
    /// </summary>
    void ShowError(string message, Exception? ex = null);

    /// <summary>
    /// 显示确认对话框。
    /// </summary>
    Task<bool> ConfirmAsync(string message);
}

/// <summary>
/// 视图注册表接口。
/// </summary>
public interface IViewRegistry
{
    /// <summary>
    /// 注册视图。
    /// </summary>
    void Register<TView, TPresenter>(string viewKey)
        where TView : class, IView
        where TPresenter : class, IPresenter;

    /// <summary>
    /// 获取视图类型。
    /// </summary>
    Type? GetViewType(string viewKey);

    /// <summary>
    /// 获取 Presenter 类型。
    /// </summary>
    Type? GetPresenterType(string viewKey);
}

/// <summary>
/// 导航服务接口。
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到视图。
    /// </summary>
    Task NavigateAsync(string viewKey, object? parameter = null);

    /// <summary>
    /// 显示对话框。
    /// </summary>
    Task<TResult?> ShowDialogAsync<TResult>(string viewKey, object? parameter = null);

    /// <summary>
    /// 返回。
    /// </summary>
    Task GoBackAsync();

    /// <summary>
    /// 获取当前视图键。
    /// </summary>
    string? CurrentViewKey { get; }
}
