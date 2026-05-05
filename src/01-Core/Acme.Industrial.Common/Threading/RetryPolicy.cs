namespace Acme.Industrial.Common.Threading;

/// <summary>
/// 重试策略，提供带重试的异步操作执行能力
/// </summary>
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;

    /// <summary>
    /// 创建重试策略
    /// </summary>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="initialDelay">初始延迟时间</param>
    /// <param name="maxDelay">最大延迟时间</param>
    /// <param name="backoffMultiplier">退避倍数（默认 2.0）</param>
    public RetryPolicy(int maxRetries = 3, TimeSpan? initialDelay = null, TimeSpan? maxDelay = null, double backoffMultiplier = 2.0)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        _backoffMultiplier = backoffMultiplier;
    }

    /// <summary>
    /// 执行带重试的操作
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var currentDelay = _initialDelay;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _maxRetries && IsTransientException(ex))
            {
                lastException = ex;
                await Task.Delay(currentDelay, cancellationToken).ConfigureAwait(false);
                currentDelay = TimeSpan.FromMilliseconds(Math.Min(currentDelay.TotalMilliseconds * _backoffMultiplier, _maxDelay.TotalMilliseconds));
            }
        }

        throw lastException ?? new InvalidOperationException("Retry policy failed");
    }

    /// <summary>
    /// 执行带重试的操作（无返回值）
    /// </summary>
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 判断是否为临时性异常（可重试）
    /// </summary>
    protected virtual bool IsTransientException(Exception ex)
    {
        return ex is TimeoutException
            || ex is OperationCanceledException
            || ex is HttpRequestException
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 重试策略扩展
/// </summary>
public static class RetryPolicyExtensions
{
    /// <summary>
    /// 创建指数退避重试策略
    /// </summary>
    public static RetryPolicy WithExponentialBackoff(int maxRetries = 3)
    {
        return new RetryPolicy(maxRetries, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30), 2.0);
    }

    /// <summary>
    /// 创建线性退避重试策略
    /// </summary>
    public static RetryPolicy WithLinearBackoff(int maxRetries = 3, TimeSpan delay = default)
    {
        return new RetryPolicy(maxRetries, delay == default ? TimeSpan.FromSeconds(1) : delay, TimeSpan.FromSeconds(10), 1.0);
    }
}
