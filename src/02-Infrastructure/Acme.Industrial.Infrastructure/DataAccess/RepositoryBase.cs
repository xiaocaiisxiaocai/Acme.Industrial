using System.Linq.Expressions;
using SqlSugar;
using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.DataAccess;
using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Infrastructure.DataAccess;

/// <summary>
/// 通用仓储基类实现。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <typeparam name="TKey">主键类型。</typeparam>
public class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, new()
{
    protected readonly SqlSugarClient _db;
    protected readonly IAppLogger _logger;

    public RepositoryBase(SqlSugarClient db, IAppLogger logger)
    {
        _db = db;
        _logger = logger;
    }

    protected SqlSugarClient Db => _db;

    // ========== 查询操作 ==========

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        try
        {
            return await _db.Queryable<TEntity>()
                .In(id)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"查询实体失败: {typeof(TEntity).Name}, Id={id}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _db.Queryable<TEntity>()
                .ToListAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取所有实体失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        try
        {
            return await _db.Queryable<TEntity>()
                .Where(predicate)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"查询实体失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _db.Queryable<TEntity>()
                .Where(predicate)
                .ToListAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"查询实体列表失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    // ========== 分页查询 ==========

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> GetPageAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Queryable<TEntity>();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync();

            // 由于 ISugarQueryable 不兼容 IQueryable，需要在内存中处理排序
            // 实际应用中建议使用 ISugarQueryable 的 OrderBy 方法
            var allItems = await query.ToListAsync();

            if (orderBy != null)
            {
                var ordered = orderBy(allItems.AsQueryable());
                allItems = ordered.ToList();
            }

            var items = allItems.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            return new PagedResult<TEntity>(items, totalCount, pageIndex, pageSize);
        }
        catch (Exception ex)
        {
            _logger.Error($"分页查询失败: {typeof(TEntity).Name}, PageIndex={pageIndex}, PageSize={pageSize}", ex);
            throw;
        }
    }

    // ========== 聚合操作 ==========

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Queryable<TEntity>();
            if (predicate != null)
            {
                return await query.Where(predicate).CountAsync();
            }
            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"统计数量失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        try
        {
            return await _db.Queryable<TEntity>()
                .Where(predicate)
                .AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"检查存在性失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Queryable<TEntity>();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            // 使用 SqlSugar 的 Sum 扩展方法
            var allItems = await query.ToListAsync();
            return allItems.AsQueryable().Sum(selector);
        }
        catch (Exception ex)
        {
            _logger.Error($"求和失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<double> AverageAsync(
        Expression<Func<TEntity, int>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Queryable<TEntity>();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            var allItems = await query.ToListAsync();
            return allItems.AsQueryable().Average(selector);
        }
        catch (Exception ex)
        {
            _logger.Error($"求平均值失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    // ========== 新增操作 ==========

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        try
        {
            var result = await _db.Insertable(entity)
                .ExecuteReturnEntityAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"新增实体失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return [];
        }

        try
        {
            await _db.Insertable(entityList).ExecuteCommandAsync();
            return entityList;
        }
        catch (Exception ex)
        {
            _logger.Error($"批量新增实体失败: {typeof(TEntity).Name}, Count={entityList.Count}", ex);
            throw;
        }
    }

    // ========== 更新操作 ==========

    /// <inheritdoc />
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        try
        {
            await _db.Updateable(entity)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"更新实体失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return;
        }

        try
        {
            await _db.Updateable(entityList)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"批量更新实体失败: {typeof(TEntity).Name}, Count={entityList.Count}", ex);
            throw;
        }
    }

    // ========== 删除操作 ==========

    /// <inheritdoc />
    public virtual async Task DeleteAsync(TKey id, CancellationToken ct = default)
    {
        try
        {
            await _db.Deleteable<TEntity>()
                .In(id)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"删除实体失败: {typeof(TEntity).Name}, Id={id}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        try
        {
            await _db.Deleteable(entity)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"删除实体失败: {typeof(TEntity).Name}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task DeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return;
        }

        try
        {
            await _db.Deleteable(entityList)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"批量删除实体失败: {typeof(TEntity).Name}, Count={entityList.Count}", ex);
            throw;
        }
    }

    // ========== 原生 SQL ==========

    /// <inheritdoc />
    public virtual async Task<int> ExecuteAsync(string sql, CancellationToken ct = default)
    {
        try
        {
            return await _db.Ado.ExecuteCommandAsync(sql);
        }
        catch (Exception ex)
        {
            _logger.Error($"执行 SQL 失败: {sql}", ex);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(string sql, CancellationToken ct = default)
        where T : class, new()
    {
        try
        {
            var result = await _db.SqlQueryable<T>(sql)
                .ToListAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"执行 SQL 查询失败: {sql}", ex);
            throw;
        }
    }

    // ========== 事务 ==========

    /// <inheritdoc />
    public virtual async Task<OperateResult> ExecuteInTransactionAsync(
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
            _logger.Error($"事务执行失败: {typeof(TEntity).Name}", ex);
            return OperateResult.Fail(ex);
        }
    }
}

/// <summary>
/// 实体主键为 Guid 的仓储基类。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public abstract class GuidRepositoryBase<TEntity> : RepositoryBase<TEntity, Guid>
    where TEntity : class, new()
{
    protected GuidRepositoryBase(SqlSugarClient db, IAppLogger logger)
        : base(db, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Queryable<TEntity>()
            .Where("Id", id)
            .FirstAsync();
    }
}
