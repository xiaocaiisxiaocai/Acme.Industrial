using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Acme.Industrial.Core.UI;
using Acme.Industrial.Core.UI.Notifications;

namespace Acme.Industrial.UI.Forms;

/// <summary>
/// 窗体基类，实现MVP模式的视图接口。
/// </summary>
public abstract class BaseForm : Form, IView
{
    #region 属性

    /// <summary>
    /// 加载指示器控件。
    /// </summary>
    protected Panel? LoadingPanel { get; private set; }

    /// <summary>
    /// 加载提示标签。
    /// </summary>
    protected Label? LoadingLabel { get; private set; }

    /// <summary>
    /// 加载进度条。
    /// </summary>
    protected ProgressBar? LoadingProgressBar { get; private set; }

    private bool _isLoading;

    /// <summary>
    /// 是否正在加载。
    /// </summary>
    protected bool IsLoading => _isLoading;

    /// <summary>
    /// 主题色。
    /// </summary>
    protected virtual System.Drawing.Color ThemeColor => System.Drawing.Color.FromArgb(0, 120, 215);

    /// <summary>
    /// 标题栏高度。
    /// </summary>
    protected virtual int TitleHeight => 32;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数。
    /// </summary>
    protected BaseForm()
    {
        InitializeBaseComponents();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化基础组件。
    /// </summary>
    private void InitializeBaseComponents()
    {
        // 设置窗体默认样式
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = System.Drawing.Color.White;

        // 启用双缓冲减少闪烁
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    /// <summary>
    /// 初始化Presenter。子类可重写。
    /// </summary>
    protected virtual void InitializePresenter()
    {
    }

    /// <summary>
    /// 初始化组件。子类实现。
    /// </summary>
    protected abstract void InitializeForm();

    #endregion

    #region IView 实现

    /// <inheritdoc />
    public virtual void ShowLoading(string? message = null)
    {
        if (_isLoading) return;
        _isLoading = true;

        if (LoadingPanel == null)
        {
            LoadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(200, 255, 255, 255),
                Visible = false
            };

            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = System.Drawing.Color.Transparent
            };
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            LoadingProgressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Width = 200,
                Height = 20,
                BackColor = System.Drawing.Color.FromArgb(230, 230, 230),
                ForeColor = ThemeColor
            };

            LoadingLabel = new Label
            {
                Text = message ?? "加载中...",
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("微软雅黑", 9F),
                ForeColor = System.Drawing.Color.FromArgb(64, 64, 64),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            container.Controls.Add(LoadingLabel, 0, 1);
            container.Controls.Add(LoadingProgressBar, 0, 1);
            LoadingPanel.Controls.Add(container);

            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Transparent
            };
            LoadingPanel.Controls.Add(overlay);

            Controls.Add(LoadingPanel);
        }

        if (LoadingLabel != null)
        {
            LoadingLabel.Text = message ?? "加载中...";
        }

        LoadingPanel.Visible = true;
        LoadingPanel.BringToFront();
        Refresh();
    }

    /// <inheritdoc />
    public virtual void HideLoading()
    {
        if (!_isLoading) return;
        _isLoading = false;

        if (LoadingPanel != null)
        {
            LoadingPanel.Visible = false;
            LoadingPanel.SendToBack();
        }
        Refresh();
    }

    /// <inheritdoc />
    public virtual void ShowMessage(string message)
    {
        MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <inheritdoc />
    public virtual void ShowError(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message}\n\n{ex.Message}" : message;
        MessageBox.Show(this, fullMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ConfirmAsync(string message)
    {
        return await Task.Run(() =>
        {
            var result = MessageBox.Show(
                this,
                message,
                "确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        });
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示Toast通知。
    /// </summary>
    public virtual void ShowToast(string message, NotificationLevel level = NotificationLevel.Info)
    {
        var icon = level switch
        {
            NotificationLevel.Success => MessageBoxIcon.Information,
            NotificationLevel.Warning => MessageBoxIcon.Warning,
            NotificationLevel.Error => MessageBoxIcon.Error,
            _ => MessageBoxIcon.Information
        };

        MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, icon);
    }

    /// <summary>
    /// 安全执行异步操作，自动处理加载状态。
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action, string? loadingMessage = null, bool showLoading = true)
    {
        if (showLoading) ShowLoading(loadingMessage);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ShowError("操作失败", ex);
        }
        finally
        {
            if (showLoading) HideLoading();
        }
    }

    /// <summary>
    /// 安全执行异步操作并返回结果。
    /// </summary>
    protected async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, string? loadingMessage = null)
    {
        ShowLoading(loadingMessage);
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            ShowError("操作失败", ex);
            return default!;
        }
        finally
        {
            HideLoading();
        }
    }

    /// <summary>
    /// 显示验证错误。
    /// </summary>
    protected void ShowValidationError(string message, Control? control = null)
    {
        ShowError(message);
        control?.Focus();
    }

    #endregion

    #region 受保护方法

    /// <summary>
    /// 创建标准按钮。
    /// </summary>
    protected Button CreateStandardButton(string text, EventHandler? click = null)
    {
        var button = new Button
        {
            Text = text,
            Size = new System.Drawing.Size(90, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeColor,
            ForeColor = System.Drawing.Color.White,
            Font = new System.Drawing.Font("微软雅黑", 9F)
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(
            Math.Min(255, ThemeColor.R + 20),
            Math.Min(255, ThemeColor.G + 20),
            Math.Min(255, ThemeColor.B + 20));

        if (click != null)
        {
            button.Click += click;
        }

        return button;
    }

    /// <summary>
    /// 创建标准文本框。
    /// </summary>
    protected TextBox CreateStandardTextBox(int width = 200)
    {
        return new TextBox
        {
            Size = new System.Drawing.Size(width, 23),
            Font = new System.Drawing.Font("微软雅黑", 9F),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>
    /// 创建标准标签。
    /// </summary>
    protected Label CreateStandardLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new System.Drawing.Font("微软雅黑", 9F),
            ForeColor = System.Drawing.Color.FromArgb(64, 64, 64)
        };
    }

    /// <summary>
    /// 添加必填标记。
    /// </summary>
    protected void AddRequiredMark(Label label)
    {
        label.Text = label.Text.TrimEnd('*') + " *";
        label.ForeColor = System.Drawing.Color.Red;
    }

    #endregion

    #region 窗体事件

    /// <inheritdoc />
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitializePresenter();
        InitializeForm();
        OnFormLoad();
    }

    /// <summary>
    /// 窗体加载时调用。
    /// </summary>
    protected virtual void OnFormLoad()
    {
    }

    /// <inheritdoc />
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        OnFormClose(e);
    }

    /// <summary>
    /// 窗体关闭时调用。
    /// </summary>
    protected virtual void OnFormClose(FormClosingEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LoadingPanel?.Dispose();
            LoadingProgressBar?.Dispose();
            LoadingLabel?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
