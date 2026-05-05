using Acme.Industrial.Core.Abstractions;

namespace Acme.Industrial.Core.UI;

/// <summary>
/// Presenter 接口。
/// </summary>
public interface IPresenter
{
    /// <summary>
    /// 视图加载完成。
    /// </summary>
    Task OnLoadAsync(CancellationToken ct);

    /// <summary>
    /// 视图卸载。
    /// </summary>
    Task OnUnloadAsync(CancellationToken ct);
}

/// <summary>
/// Presenter 基类。
/// </summary>
/// <typeparam name="TView">视图类型。</typeparam>
public abstract class PresenterBase<TView> : IPresenter where TView : IView
{
    /// <summary>
    /// 视图实例。
    /// </summary>
    protected TView View { get; }

    /// <summary>
    /// 服务解析器。
    /// </summary>
    protected IServiceResolver Services { get; }

    /// <summary>
    /// 构造函数。
    /// </summary>
    protected PresenterBase(TView view, IServiceResolver services)
    {
        View = view;
        Services = services;
    }

    /// <summary>
    /// 视图加载完成。
    /// </summary>
    public virtual Task OnLoadAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// 视图卸载。
    /// </summary>
    public virtual Task OnUnloadAsync(CancellationToken ct) => Task.CompletedTask;
}
