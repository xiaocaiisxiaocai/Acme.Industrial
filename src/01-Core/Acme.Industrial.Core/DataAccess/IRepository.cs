using System.Linq.Expressions;
using Acme.Industrial.Core.Results;

namespace Acme.Industrial.Core.DataAccess;

/// <summary>
/// 通用仓储接口。
/// </summary>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    // ========== 查询操作 ==========

    /// <summary>
    /// 根据主键获取实体。
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    /// <summary>
    /// 获取所有实体。
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取满足条件的第一个实体。
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// 获取满足条件的所有实体。
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    // ========== 分页查询 ==========

    /// <summary>
    /// 分页查询。
    /// </summary>
    Task<PagedResult<TEntity>> GetPageAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default);

    // ========== 聚合操作 ==========

    /// <summary>
    /// 统计数量。
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>
    /// 检查是否存在。
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// 聚合查询 - 求和。
    /// </summary>
    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>
    /// 聚合查询 - 平均值。
    /// </summary>
    Task<double> AverageAsync(Expression<Func<TEntity, int>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    // ========== 新增操作 ==========

    /// <summary>
    /// 新增实体。
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// 批量新增实体。
    /// </summary>
    Task<IReadOnlyList<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken ct = default);

    // ========== 更新操作 ==========

    /// <summary>
    /// 更新实体。
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// 批量更新实体。
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ========== 删除操作 ==========

    /// <summary>
    /// 根据主键删除。
    /// </summary>
    Task DeleteAsync(TKey id, CancellationToken ct = default);

    /// <summary>
    /// 删除实体。
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// 批量删除实体。
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ========== 原生 SQL ==========

    /// <summary>
    /// 执行 SQL 命令。
    /// </summary>
    Task<int> ExecuteAsync(string sql, CancellationToken ct = default);

    /// <summary>
    /// 执行 SQL 查询。
    /// </summary>
    Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(string sql, CancellationToken ct = default)
        where T : class, new();

    // ========== 事务 ==========

    /// <summary>
    /// 在事务中执行操作。
    /// </summary>
    Task<OperateResult> ExecuteInTransactionAsync(Func<Task> action,
        CancellationToken ct = default);
}
