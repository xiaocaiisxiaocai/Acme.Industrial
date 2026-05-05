using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Abstractions;

/// <summary>
/// 点位订阅接口 - 支持点位变化订阅和死区过滤。
/// </summary>
public interface ITagSubscriber : IDisposable
{
    /// <summary>
    /// 订阅点位变化。
    /// </summary>
    /// <param name="tags">要订阅的点位列表。</param>
    /// <param name="onValueChanged">值变化回调。</param>
    /// <returns>可支配的订阅句柄，用于取消订阅。</returns>
    IDisposable Subscribe(IEnumerable<Tag> tags, Action<TagValue> onValueChanged);

    /// <summary>
    /// 取消所有订阅。
    /// </summary>
    void UnsubscribeAll();

    /// <summary>
    /// 获取当前订阅的点位数量。
    /// </summary>
    int SubscribedTagCount { get; }

    /// <summary>
    /// 检查是否正在订阅指定点位。
    /// </summary>
    bool IsSubscribed(string tagName);
}
