namespace Acme.Industrial.Core.UI.Notifications;

/// <summary>
/// 通知级别。
/// </summary>
public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// 通知服务接口。
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 显示通知。
    /// </summary>
    void Show(string message, NotificationLevel level = NotificationLevel.Info,
        TimeSpan? duration = null);

    /// <summary>
    /// 显示确认对话框。
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>
    /// 显示输入对话框。
    /// </summary>
    Task<string?> PromptAsync(string title, string message, string? defaultValue = null);

    /// <summary>
    /// 显示 toast 通知。
    /// </summary>
    void ShowToast(string message, NotificationLevel level = NotificationLevel.Info,
        TimeSpan? duration = null);
}
