using SqlSugar;
using Acme.Industrial.Core.DataAccess;

namespace Acme.Industrial.Infrastructure.DataAccess;

/// <summary>
/// 数据库初始化扩展方法。
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// 创建 SqlSugar 客户端。
    /// </summary>
    public static SqlSugarClient CreateSqlSugarClient(DatabaseOptions options)
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

        return new SqlSugarClient(config);
    }

    /// <summary>
    /// 从连接字符串创建 SqlSugar 客户端。
    /// </summary>
    public static SqlSugarClient CreateSqlSugarClient(string connectionString, DatabaseType dbType = DatabaseType.SqlServer)
    {
        var options = new DatabaseOptions
        {
            ConnectionString = connectionString,
            DbType = dbType
        };
        return CreateSqlSugarClient(options);
    }
}
