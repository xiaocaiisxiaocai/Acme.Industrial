using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.UI;

namespace Acme.Industrial.UI.Navigation;

/// <summary>
/// 导航服务接口扩展，用于WinForms特定功能。
/// </summary>
public interface IWinFormsNavigationService : INavigationService
{
    /// <summary>
    /// 获取或设置主窗体。
    /// </summary>
    Form? MainForm { get; set; }

    /// <summary>
    /// 获取当前内容面板。
    /// </summary>
    Panel? ContentPanel { get; set; }

    /// <summary>
    /// 设置内容面板。
    /// </summary>
    void SetContentPanel(Panel panel);
}

/// <summary>
/// 导航服务实现。
/// </summary>
public class NavigationService : IWinFormsNavigationService
{
    private readonly IServiceResolver _serviceResolver;
    private readonly IViewRegistry _viewRegistry;
    private readonly Stack<string> _navigationStack = new();
    private Form? _mainForm;
    private Panel? _contentPanel;

    public Form? MainForm
    {
        get => _mainForm;
        set => _mainForm = value;
    }

    public Panel? ContentPanel
    {
        get => _contentPanel;
        set => _contentPanel = value;
    }

    public string? CurrentViewKey => _navigationStack.Count > 0 ? _navigationStack.Peek() : null;

    public NavigationService(IServiceResolver serviceResolver, IViewRegistry viewRegistry)
    {
        _serviceResolver = serviceResolver;
        _viewRegistry = viewRegistry;
    }

    /// <inheritdoc />
    public void SetContentPanel(Panel panel)
    {
        _contentPanel = panel;
    }

    /// <inheritdoc />
    public async Task NavigateAsync(string viewKey, object? parameter = null)
    {
        if (_contentPanel == null)
        {
            throw new InvalidOperationException("ContentPanel未设置，请先调用SetContentPanel");
        }

        var viewType = _viewRegistry.GetViewType(viewKey);
        if (viewType == null)
        {
            throw new InvalidOperationException($"未找到视图: {viewKey}");
        }

        // 清理当前内容
        _contentPanel.Controls.Clear();

        // 创建视图实例
        var view = CreateView(viewType);
        if (view == null)
        {
            throw new InvalidOperationException($"无法创建视图实例: {viewKey}");
        }

        Control? control = null;
        Form? form = null;

        // 如果是Form，设置属性
        if (view is Form f)
        {
            form = f;
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Size = _contentPanel.Size;
            control = form;
        }
        else if (view is Control c)
        {
            control = c;
        }

        // 获取Presenter并调用OnLoadAsync
        var presenterType = _viewRegistry.GetPresenterType(viewKey);
        if (presenterType != null)
        {
            try
            {
                var resolveMethod = typeof(IServiceResolver).GetMethod("TryResolve")?.MakeGenericMethod(presenterType);
                var presenter = resolveMethod?.Invoke(_serviceResolver, null);
                if (presenter != null)
                {
                    var onLoadMethod = presenterType.GetMethod("OnLoadAsync");
                    onLoadMethod?.Invoke(presenter, new object[] { CancellationToken.None });
                }
            }
            catch
            {
                // 忽略Presenter调用错误
            }
        }

        // 添加到内容面板
        if (control != null)
        {
            _contentPanel.Controls.Add(control);
        }

        // 压入导航栈
        _navigationStack.Push(viewKey);
    }

    /// <inheritdoc />
    public Task<TResult?> ShowDialogAsync<TResult>(string viewKey, object? parameter = null)
    {
        var viewType = _viewRegistry.GetViewType(viewKey);
        if (viewType == null)
        {
            throw new InvalidOperationException($"未找到视图: {viewKey}");
        }

        var view = CreateView(viewType);
        if (view is not Form form)
        {
            throw new InvalidOperationException($"对话框必须是Form类型: {viewKey}");
        }

        // 设置为对话框
        form.StartPosition = FormStartPosition.CenterParent;
        form.TopMost = true;

        // 获取Presenter并调用OnLoadAsync
        var presenterType = _viewRegistry.GetPresenterType(viewKey);
        if (presenterType != null)
        {
            try
            {
                var resolveMethod = typeof(IServiceResolver).GetMethod("TryResolve")?.MakeGenericMethod(presenterType);
                var presenter = resolveMethod?.Invoke(_serviceResolver, null);
                if (presenter != null)
                {
                    var onLoadMethod = presenterType.GetMethod("OnLoadAsync");
                    onLoadMethod?.Invoke(presenter, new object[] { CancellationToken.None });
                }
            }
            catch
            {
                // 忽略Presenter调用错误
            }
        }

        // 显示对话框
        form.ShowDialog();

        // 尝试获取结果
        if (view is IDialogResultProvider<TResult> resultProvider)
        {
            return Task.FromResult(resultProvider.DialogResult);
        }

        return Task.FromResult(default(TResult?));
    }

    /// <inheritdoc />
    public async Task GoBackAsync()
    {
        if (_contentPanel == null || _navigationStack.Count <= 1)
        {
            return;
        }

        // 弹出当前视图
        _navigationStack.Pop();

        // 获取上一个视图
        var previousKey = _navigationStack.Peek();
        var viewType = _viewRegistry.GetViewType(previousKey);
        if (viewType == null)
        {
            return;
        }

        // 清理当前内容
        _contentPanel.Controls.Clear();

        // 创建视图实例
        var view = CreateView(viewType);
        if (view == null)
        {
            return;
        }

        Control? control = null;
        Form? form = null;

        // 如果是Form，设置属性
        if (view is Form f)
        {
            form = f;
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Size = _contentPanel.Size;
            control = form;
        }
        else if (view is Control c)
        {
            control = c;
        }

        // 获取Presenter并调用OnLoadAsync
        var presenterType = _viewRegistry.GetPresenterType(previousKey);
        if (presenterType != null)
        {
            try
            {
                var resolveMethod = typeof(IServiceResolver).GetMethod("TryResolve")?.MakeGenericMethod(presenterType);
                var presenter = resolveMethod?.Invoke(_serviceResolver, null);
                if (presenter != null)
                {
                    var onLoadMethod = presenterType.GetMethod("OnLoadAsync");
                    onLoadMethod?.Invoke(presenter, new object[] { CancellationToken.None });
                }
            }
            catch
            {
                // 忽略Presenter调用错误
            }
        }

        // 添加到内容面板
        if (control != null)
        {
            _contentPanel.Controls.Add(control);
        }
    }

    /// <summary>
    /// 创建视图实例。
    /// </summary>
    private static object? CreateView(Type viewType)
    {
        // 直接创建实例
        try
        {
            return Activator.CreateInstance(viewType);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 对话框结果提供器接口。
/// </summary>
public interface IDialogResultProvider<TResult>
{
    /// <summary>
    /// 对话框结果。
    /// </summary>
    TResult? DialogResult { get; }
}
