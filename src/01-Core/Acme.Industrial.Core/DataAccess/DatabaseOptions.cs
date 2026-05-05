namespace Acme.Industrial.Core.DataAccess;

/// <summary>
/// 数据访问层接口定义。
/// </summary>
public static class DataAccessDefaults
{
    /// <summary>
    /// 默认命令超时时间（秒）。
    /// </summary>
    public const int DefaultCommandTimeout = 30;
}

/// <summary>
/// 数据库类型枚举。
/// </summary>
public enum DatabaseType
{
    SqlServer,
    MySql,
    PostgreSql,
    Sqlite,
    Oracle
}

/// <summary>
/// 数据库配置选项。
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 数据库类型。
    /// </summary>
    public DatabaseType DbType { get; set; } = DatabaseType.SqlServer;

    /// <summary>
    /// 命令超时时间（秒）。
    /// </summary>
    public int CommandTimeout { get; set; } = DataAccessDefaults.DefaultCommandTimeout;

    /// <summary>
    /// 是否启用日志。
    /// </summary>
    public bool EnableLog { get; set; } = false;

    /// <summary>
    /// 连接池最小大小。
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// 连接池最大大小。
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;
}
