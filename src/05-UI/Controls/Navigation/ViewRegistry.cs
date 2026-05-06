using System;
using System.Collections.Generic;
using System.Linq;
using Acme.Industrial.Core.UI;

namespace Acme.Industrial.UI.Navigation;

/// <summary>
/// 视图注册表实现。
/// </summary>
public class ViewRegistry : IViewRegistry
{
    private readonly Dictionary<string, Type> _viewTypes = new();
    private readonly Dictionary<string, Type> _presenterTypes = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Register<TView, TPresenter>(string viewKey) where TView : class, IView where TPresenter : class, IPresenter
    {
        lock (_lock)
        {
            _viewTypes[viewKey] = typeof(TView);
            _presenterTypes[viewKey] = typeof(TPresenter);
        }
    }

    /// <inheritdoc />
    public Type? GetViewType(string viewKey)
    {
        lock (_lock)
        {
            return _viewTypes.TryGetValue(viewKey, out var type) ? type : null;
        }
    }

    /// <inheritdoc />
    public Type? GetPresenterType(string viewKey)
    {
        lock (_lock)
        {
            return _presenterTypes.TryGetValue(viewKey, out var type) ? type : null;
        }
    }

    /// <summary>
    /// 获取所有已注册的视图键。
    /// </summary>
    public IEnumerable<string> GetAllViewKeys()
    {
        lock (_lock)
        {
            return _viewTypes.Keys.ToList();
        }
    }
}
