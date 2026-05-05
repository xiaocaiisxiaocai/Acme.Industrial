using SqlSugar;
using Acme.Industrial.Core.DataAccess;

namespace Acme.Industrial.Infrastructure.DataAccess;

/// <summary>
/// SqlSugar 数据库上下文包装器。
/// </summary>
public class SqlSugarDbContext : IDisposable
{
    private readonly SqlSugarClient _client;
    private readonly DatabaseOptions _options;
    private bool _disposed;

    public SqlSugarDbContext(DatabaseOptions options)
    {
        _options = options;
        _client = CreateClient(options);
    }

    public SqlSugarClient Client => _client;
    public DatabaseOptions Options => _options;

    private static SqlSugarClient CreateClient(DatabaseOptions options)
    {
        var dbType = options.DbType switch
        {
            DatabaseType.SqlServer => DbType.SqlServer,
            DatabaseType.MySql => DbType.MySql,
            DatabaseType.PostgreSql => DbType.PostgreSQL,
            DatabaseType.Sqlite => DbType.Sqlite,
            DatabaseType.Oracle => DbType.Oracle,
            _ => DbType.SqlServer
        };

        var config = new ConnectionConfig
        {
            ConnectionString = options.ConnectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        };

        var client = new SqlSugarClient(config);

        if (options.EnableLog)
        {
            client.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine($"[SQL] {sql}");
            };
        }

        return client;
    }

    /// <summary>
    /// 开始事务。
    /// </summary>
    public void BeginTran()
    {
        _client.BeginTran();
    }

    /// <summary>
    /// 提交事务。
    /// </summary>
    public void CommitTran()
    {
        _client.CommitTran();
    }

    /// <summary>
    /// 回滚事务。
    /// </summary>
    public void RollbackTran()
    {
        _client.RollbackTran();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _client.Dispose();
    }
}
