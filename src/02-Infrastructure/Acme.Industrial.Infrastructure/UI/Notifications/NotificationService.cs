using System.Windows.Forms;
using Acme.Industrial.Core.UI.Notifications;

namespace Acme.Industrial.Infrastructure.UI.Notifications;

/// <summary>
/// WinForms 通知服务实现。
/// </summary>
public class NotificationService : INotificationService
{
    private readonly string _applicationName;

    public NotificationService(string applicationName = "工业监控系统")
    {
        _applicationName = applicationName;
    }

    public void Show(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
    {
        var caption = GetCaption(level);
        var icon = GetIcon(level);
        MessageBox.Show(message, caption, MessageBoxButtons.OK, icon);
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        return Task.FromResult(result == DialogResult.Yes);
    }

    public Task<string?> PromptAsync(string title, string message, string? defaultValue = null)
    {
        using var dialog = new InputDialog(title, message, defaultValue);
        var result = dialog.ShowDialog();
        return Task.FromResult(result == DialogResult.OK ? dialog.InputValue : null);
    }

    public void ShowToast(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
    {
        Show(message, level, duration);
    }

    private string GetCaption(NotificationLevel level) => level switch
    {
        NotificationLevel.Info => "提示",
        NotificationLevel.Success => "成功",
        NotificationLevel.Warning => "警告",
        NotificationLevel.Error => "错误",
        _ => _applicationName
    };

    private MessageBoxIcon GetIcon(NotificationLevel level) => level switch
    {
        NotificationLevel.Info => MessageBoxIcon.Information,
        NotificationLevel.Success => MessageBoxIcon.Information,
        NotificationLevel.Warning => MessageBoxIcon.Warning,
        NotificationLevel.Error => MessageBoxIcon.Error,
        _ => MessageBoxIcon.Information
    };
}

/// <summary>
/// 输入对话框。
/// </summary>
internal class InputDialog : Form
{
    public string InputValue => textBox.Text;

    private readonly TextBox textBox;

    public InputDialog(string title, string message, string? defaultValue = null)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AcceptButton = buttonOK;
        CancelButton = buttonCancel;

        var label = new Label
        {
            Text = message,
            Location = new System.Drawing.Point(12, 12),
            AutoSize = true
        };

        textBox = new TextBox
        {
            Text = defaultValue ?? string.Empty,
            Location = new System.Drawing.Point(12, 40),
            Width = 300
        };

        buttonOK = new Button
        {
            Text = "确定",
            Location = new System.Drawing.Point(138, 75),
            Width = 75,
            Height = 23,
            DialogResult = DialogResult.OK
        };

        buttonCancel = new Button
        {
            Text = "取消",
            Location = new System.Drawing.Point(219, 75),
            Width = 75,
            Height = 23,
            DialogResult = DialogResult.Cancel
        };

        ClientSize = new System.Drawing.Size(324, 110);
        Controls.AddRange(new Control[] { label, textBox, buttonOK, buttonCancel });

        textBox.SelectAll();
        textBox.Focus();
    }

    private Button buttonOK = null!;
    private Button buttonCancel = null!;
}

/// <summary>
/// 托盘通知服务实现（使用 NotifyIcon）。
/// </summary>
public class TrayNotificationService : INotificationService, IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly INotificationService _innerService;
    private bool _disposed;

    public TrayNotificationService(INotificationService innerService, string title = "工业监控")
    {
        _innerService = innerService;
        _notifyIcon = new NotifyIcon
        {
            Text = title,
            Visible = false
        };
        _notifyIcon.BalloonTipClicked += (_, _) => OnBalloonClicked?.Invoke();
    }

    public event Action? OnBalloonClicked;

    public void Show(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
    {
        _innerService.Show(message, level, duration);
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        return _innerService.ConfirmAsync(title, message);
    }

    public Task<string?> PromptAsync(string title, string message, string? defaultValue = null)
    {
        return _innerService.PromptAsync(title, message, defaultValue);
    }

    public void ShowToast(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
    {
        ShowBalloon(message, level, duration);
    }

    public void ShowBalloon(string message, NotificationLevel level, TimeSpan? duration = null)
    {
        _notifyIcon.Visible = true;
        _notifyIcon.BalloonTipIcon = GetIcon(level);
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipTitle = GetCaption(level);
        _notifyIcon.ShowBalloonTip((int)(duration?.TotalMilliseconds ?? 3000));
    }

    public void SetIcon(System.Drawing.Icon icon)
    {
        _notifyIcon.Icon = icon;
    }

    public void SetContextMenu(ContextMenuStrip menu)
    {
        _notifyIcon.ContextMenuStrip = menu;
    }

    private string GetCaption(NotificationLevel level) => level switch
    {
        NotificationLevel.Info => "提示",
        NotificationLevel.Success => "成功",
        NotificationLevel.Warning => "警告",
        NotificationLevel.Error => "错误",
        _ => "通知"
    };

    private ToolTipIcon GetIcon(NotificationLevel level) => level switch
    {
        NotificationLevel.Info => ToolTipIcon.Info,
        NotificationLevel.Success => ToolTipIcon.Info,
        NotificationLevel.Warning => ToolTipIcon.Warning,
        NotificationLevel.Error => ToolTipIcon.Error,
        _ => ToolTipIcon.None
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
