using System;
using System.Runtime.CompilerServices;

namespace Acme.Industrial.Core.Logging;

/// <summary>
/// 日志级别。
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

/// <summary>
/// 应用程序日志接口。
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// 是否启用指定级别。
    /// </summary>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// 记录日志。
    /// </summary>
    void Log(LogLevel level, string message, Exception? ex = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0);

    /// <summary>
    /// 跟踪级别日志。
    /// </summary>
    void Trace(string message);

    /// <summary>
    /// 调试级别日志。
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// 信息级别日志。
    /// </summary>
    void Info(string message);

    /// <summary>
    /// 警告级别日志。
    /// </summary>
    void Warn(string message, Exception? ex = null);

    /// <summary>
    /// 错误级别日志。
    /// </summary>
    void Error(string message, Exception? ex = null);

    /// <summary>
    /// 致命级别日志。
    /// </summary>
    void Fatal(string message, Exception? ex = null);
}

/// <summary>
/// 日志工厂接口。
/// </summary>
public interface IAppLoggerFactory
{
    /// <summary>
    /// 创建日志器。
    /// </summary>
    IAppLogger CreateLogger(string category);

    /// <summary>
    /// 创建日志器。
    /// </summary>
    IAppLogger CreateLogger<T>();
}
