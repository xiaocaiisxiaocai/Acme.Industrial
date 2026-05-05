using SqlSugar;
using Acme.Industrial.Core.DataAccess;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Infrastructure.DataAccess;

/// <summary>
/// 工作单元实现。
/// </summary>
public class UnitOfWork : IUnitOfWork, IUnitOfWorkAsync
{
    private readonly SqlSugarClient _db;
    private readonly IAppLogger _logger;
    private readonly IServiceResolver _resolver;
    private readonly Dictionary<Type, object> _repositories = new();
    private bool _isTransactionStarted;
    private bool _disposed;

    public UnitOfWork(SqlSugarClient db, IAppLogger logger, IServiceResolver resolver)
    {
        _db = db;
        _logger = logger;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public bool IsTransactionStarted => _isTransactionStarted;

    /// <inheritdoc />
    public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class
    {
        var key = typeof(IRepository<TEntity, TKey>);

        if (_repositories.TryGetValue(key, out var existing))
        {
            return (IRepository<TEntity, TKey>)existing;
        }

        var repository = _resolver.Resolve<IRepository<TEntity, TKey>>();
        _repositories[key] = repository;
        return repository;
    }

    /// <inheritdoc />
    public Task BeginAsync(CancellationToken ct = default)
    {
        if (_isTransactionStarted)
        {
            throw new InvalidOperationException("事务已开启");
        }

        _db.BeginTran();
        _isTransactionStarted = true;
        _logger.Debug("数据库事务已开启");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<OperateResult> CommitAsync(CancellationToken ct = default)
    {
        if (!_isTransactionStarted)
        {
            return Task.FromResult(OperateResult.Fail("没有开启的事务"));
        }

        try
        {
            _db.CommitTran();
            _isTransactionStarted = false;
            _logger.Debug("数据库事务已提交");
            return Task.FromResult(OperateResult.Ok());
        }
        catch (Exception ex)
        {
            _logger.Error("提交事务失败", ex);
            return Task.FromResult(OperateResult.Fail(ex));
        }
    }

    /// <inheritdoc />
    public Task<OperateResult> RollbackAsync(CancellationToken ct = default)
    {
        if (!_isTransactionStarted)
        {
            return Task.FromResult(OperateResult.Ok());
        }

        try
        {
            _db.RollbackTran();
            _isTransactionStarted = false;
            _logger.Debug("数据库事务已回滚");
            return Task.FromResult(OperateResult.Ok());
        }
        catch (Exception ex)
        {
            _logger.Error("回滚事务失败", ex);
            return Task.FromResult(OperateResult.Fail(ex));
        }
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public async Task<OperateResult> ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                await action();
            });

            return result.IsSuccess
                ? OperateResult.Ok()
                : OperateResult.Fail(result.ErrorMessage ?? "事务执行失败");
        }
        catch (Exception ex)
        {
            _logger.Error("事务执行失败", ex);
            return OperateResult.Fail(ex);
        }
    }

    /// <inheritdoc />
    public Task<OperateResult> ExecuteInTransactionAsync(
        Func<IUnitOfWork, Task> action,
        CancellationToken ct = default)
    {
        return ExecuteInTransactionAsync(() => action(this), ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isTransactionStarted)
        {
            _db.RollbackTran();
            _isTransactionStarted = false;
        }

        _repositories.Clear();
    }
}

/// <summary>
/// 工作单元工厂。
/// </summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}

/// <summary>
/// SqlSugar 工作单元工厂实现。
/// </summary>
public class SqlSugarUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly SqlSugarClient _db;
    private readonly IAppLogger _logger;
    private readonly IServiceResolver _resolver;

    public SqlSugarUnitOfWorkFactory(
        SqlSugarClient db,
        IAppLogger logger,
        IServiceResolver resolver)
    {
        _db = db;
        _logger = logger;
        _resolver = resolver;
    }

    public IUnitOfWork Create()
    {
        return new UnitOfWork(_db, _logger, _resolver);
    }
}
