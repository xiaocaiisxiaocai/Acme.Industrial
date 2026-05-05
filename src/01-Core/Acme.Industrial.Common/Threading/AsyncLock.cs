using System.Threading;
using System.Threading.Tasks;

namespace Acme.Industrial.Common.Threading;

/// <summary>
/// 异步锁，提供异步环境下的互斥访问
/// </summary>
public sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly CancellationTokenSource _externalCts;

    public AsyncLock()
    {
        _semaphore = new SemaphoreSlim(1, 1);
        _externalCts = new CancellationTokenSource();
    }

    /// <summary>
    /// 异步获取锁
    /// </summary>
    public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _externalCts.Token);
        await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
        return new LockReleaser(_semaphore);
    }

    /// <summary>
    /// 尝试同步获取锁
    /// </summary>
    public bool TryLock(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return _semaphore.Wait(timeout, cancellationToken);
    }

    /// <summary>
    /// 尝试异步获取锁
    /// </summary>
    public async Task<bool> TryLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _externalCts.Token);
        return await _semaphore.WaitAsync(timeout, cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// 同步获取锁（阻塞）
    /// </summary>
    public void Lock(CancellationToken cancellationToken = default)
    {
        _semaphore.Wait(cancellationToken);
    }

    public void Dispose()
    {
        _externalCts.Cancel();
        _semaphore.Dispose();
        _externalCts.Dispose();
    }

    /// <summary>
    /// 锁释放器
    /// </summary>
    private sealed class LockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public LockReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _semaphore.Release();
            }
        }
    }
}
