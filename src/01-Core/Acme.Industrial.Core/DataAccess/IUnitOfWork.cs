using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Core.DataAccess;

/// <summary>
/// 工作单元接口。
/// 提供统一的事务管理和仓储访问。
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 获取指定类型的仓储。
    /// </summary>
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class;

    /// <summary>
    /// 开始事务。
    /// </summary>
    Task BeginAsync(CancellationToken ct = default);

    /// <summary>
    /// 提交事务。
    /// </summary>
    Task<OperateResult> CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// 回滚事务。
    /// </summary>
    Task<OperateResult> RollbackAsync(CancellationToken ct = default);

    /// <summary>
    /// 是否已开启事务。
    /// </summary>
    bool IsTransactionStarted { get; }

    /// <summary>
    /// 保存所有更改。
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// 异步工作单元接口，支持链式调用。
/// </summary>
public interface IUnitOfWorkAsync : IUnitOfWork
{
    /// <summary>
    /// 在事务中执行操作。
    /// </summary>
    Task<OperateResult> ExecuteInTransactionAsync(
        Func<IUnitOfWork, Task> action,
        CancellationToken ct = default);
}
