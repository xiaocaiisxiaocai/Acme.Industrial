using System.Collections.Concurrent;
using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Core;

/// <summary>
/// 点位订阅实现 - 支持死区过滤和批量采集。
/// </summary>
public class TagSubscriptionService : ITagSubscriber
{
    private readonly ConcurrentDictionary<string, TagSubscription> _subscriptions = new();
    private readonly ConcurrentDictionary<string, TagValue> _lastValues = new();
    private readonly object _lockObj = new();
    private bool _disposed;

    /// <summary>
    /// 订阅点位变化。
    /// </summary>
    public IDisposable Subscribe(IEnumerable<Tag> tags, Action<TagValue> onValueChanged)
    {
        ThrowIfDisposed();
        var tagList = tags.ToList();
        var subscription = new TagSubscription(tagList, onValueChanged);

        foreach (var tag in tagList)
        {
            _subscriptions.TryAdd(tag.Name, subscription);
        }

        return new SubscriptionHandle(this, tagList.Select(t => t.Name).ToList());
    }

    /// <summary>
    /// 取消所有订阅。
    /// </summary>
    public void UnsubscribeAll()
    {
        ThrowIfDisposed();
        _subscriptions.Clear();
        _lastValues.Clear();
    }

    /// <summary>
    /// 获取当前订阅的点位数量。
    /// </summary>
    public int SubscribedTagCount => _subscriptions.Count;

    /// <summary>
    /// 检查是否正在订阅指定点位。
    /// </summary>
    public bool IsSubscribed(string tagName) => _subscriptions.ContainsKey(tagName);

    /// <summary>
    /// 处理采集到的值，应用死区过滤。
    /// </summary>
    internal void OnTagValueReceived(TagValue value)
    {
        if (!_subscriptions.TryGetValue(value.TagName, out var subscription))
            return;

        if (ShouldNotify(value))
        {
            subscription.Callback(value);
        }
    }

    /// <summary>
    /// 处理批量采集结果。
    /// </summary>
    internal void OnBatchValuesReceived(IEnumerable<TagValue> values)
    {
        foreach (var value in values)
        {
            OnTagValueReceived(value);
        }
    }

    /// <summary>
    /// 检查点位是否应该通知（死区过滤）。
    /// </summary>
    private bool ShouldNotify(TagValue newValue)
    {
        if (!_lastValues.TryGetValue(newValue.TagName, out var lastValue))
        {
            // 首次值总是通知
            _lastValues[newValue.TagName] = newValue;
            return true;
        }

        // 质量变化时通知
        if (lastValue.Quality != newValue.Quality)
        {
            _lastValues[newValue.TagName] = newValue;
            return true;
        }

        // 坏质量不通知
        if (newValue.Quality != TagQuality.Good)
        {
            return false;
        }

        // 死区检查
        if (TryGetDeadBand(newValue.TagName, out var deadBand))
        {
            if (deadBand > 0)
            {
                var oldNum = Convert.ToDouble(lastValue.Value);
                var newNum = Convert.ToDouble(newValue.Value);
                var diff = Math.Abs(newNum - oldNum);
                if (diff <= deadBand)
                {
                    return false;
                }
            }
        }

        _lastValues[newValue.TagName] = newValue;
        return true;
    }

    private bool TryGetDeadBand(string tagName, out double deadBand)
    {
        if (_subscriptions.TryGetValue(tagName, out var subscription))
        {
            var tag = subscription.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag != null)
            {
                deadBand = tag.DeadBand;
                return true;
            }
        }
        deadBand = 0;
        return false;
    }

    private void Unsubscribe(IEnumerable<string> tagNames)
    {
        foreach (var name in tagNames)
        {
            _subscriptions.TryRemove(name, out _);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TagSubscriptionService));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscriptions.Clear();
        _lastValues.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 订阅包装类。
    /// </summary>
    private class TagSubscription
    {
        public IReadOnlyList<Tag> Tags { get; }
        public Action<TagValue> Callback { get; }

        public TagSubscription(IReadOnlyList<Tag> tags, Action<TagValue> callback)
        {
            Tags = tags;
            Callback = callback;
        }
    }

    /// <summary>
    /// 订阅句柄 - 用于取消订阅。
    /// </summary>
    private class SubscriptionHandle : IDisposable
    {
        private readonly TagSubscriptionService _service;
        private readonly List<string> _tagNames;
        private bool _disposed;

        public SubscriptionHandle(TagSubscriptionService service, List<string> tagNames)
        {
            _service = service;
            _tagNames = tagNames;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _service.Unsubscribe(_tagNames);
            GC.SuppressFinalize(this);
        }
    }
}
