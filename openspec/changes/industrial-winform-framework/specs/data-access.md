# Data Access - 数据访问层规格

## Overview

数据访问层提供统一的数据持久化抽象，支持多种数据库实现。

## Requirements

### R-1: IRepository 接口

```csharp
public interface IRepository<TEntity, TKey> where TEntity : class
{
    // 查询
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    // 分页
    Task<PagedResult<TEntity>> GetPageAsync(
        int pageIndex, int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default);

    // 聚合
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    // 新增
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> AddRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken ct = default);

    // 更新
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // 删除
    Task DeleteAsync(TKey id, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // 执行
    Task<int> ExecuteAsync(string sql, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(string sql, CancellationToken ct = default)
        where T : class;

    // 事务
    Task<OperateResult> ExecuteInTransactionAsync(
        Func<Task> action, CancellationToken ct = default);
}
```

### R-2: PagedResult 分页结果

```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 0;
    public bool HasNext => PageIndex < TotalPages - 1;
}
```

### R-3: IUnitOfWork 工作单元

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class;
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

### R-4: ISqlSugarClient 抽象

```csharp
public interface ISqlSugarClient
{
    ITenant Tenant { get; }
    IAdo Ado { get; }

    ISugarQueryable<T> Queryable<T>() where T : class, new();
    IInsertable<T> Insertable<T>(T entity) where T : class;
    IUpdateable<T> Updateable<T>(T entity) where T : class;
    IDeleteable<T> Deleteable<T>() where T : class;

    Task<T> InsertReturnEntityAsync<T>(T entity, CancellationToken ct = default)
        where T : class;
    Task<int> Insertable<T>( IEnumerable<T> entities).ExecuteCommandAsync(
        CancellationToken ct = default);
    Task<int> Updateable<T>(IEnumerable<T> entities).ExecuteCommandAsync(
        CancellationToken ct = default);
    Task<int> Deleteable<T>().Where(Expression<Func<T, bool>>).ExecuteCommandAsync(
        CancellationToken ct = default);
}
```

### R-5: RepositoryBase 基类

```csharp
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly ISqlSugarClient _db;
    protected readonly IAppLogger _logger;

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    // ... 其他方法
}
```

### R-6: 数据库配置

```csharp
public class DatabaseOptions
{
    public string ConnectionString { get; set; }
    public DatabaseType DbType { get; set; } = DatabaseType.SqlServer;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableLog { get; set; } = false;
}

public enum DatabaseType
{
    SqlServer,
    MySql,
    PostgreSql,
    Sqlite,
    Oracle
}
```

---

## Acceptance Criteria

- [ ] IRepository 支持所有 CRUD 操作
- [ ] 支持分页查询
- [ ] 支持表达式树查询
- [ ] 支持批量操作
- [ ] 支持事务
- [ ] 支持至少 SqlServer、MySql、PostgreSql、Sqlite
- [ ] RepositoryBase 提供通用实现
- [ ] 所有操作支持 CancellationToken
- [ ] 集成日志记录

---

## Dependencies

- `core-abstractions`
