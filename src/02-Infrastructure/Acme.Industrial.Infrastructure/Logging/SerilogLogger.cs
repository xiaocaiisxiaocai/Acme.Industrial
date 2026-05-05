using System;
using System.IO;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;
using Acme.Industrial.Core.Logging;
using ThisLog = Serilog.Log;

namespace Acme.Industrial.Infrastructure.Logging;

/// <summary>
/// Serilog 日志实现。
/// </summary>
public class SerilogLogger : IAppLogger
{
    private readonly Serilog.ILogger _logger;
    private readonly string _category;

    public SerilogLogger(string category)
    {
        _category = category;
        _logger = ThisLog.ForContext("Category", category);
    }

    public bool IsEnabled(LogLevel level) => level switch
    {
        LogLevel.Trace => _logger.IsEnabled(LogEventLevel.Verbose),
        LogLevel.Debug => _logger.IsEnabled(LogEventLevel.Debug),
        LogLevel.Info => _logger.IsEnabled(LogEventLevel.Information),
        LogLevel.Warn => _logger.IsEnabled(LogEventLevel.Warning),
        LogLevel.Error => _logger.IsEnabled(LogEventLevel.Error),
        LogLevel.Fatal => _logger.IsEnabled(LogEventLevel.Fatal),
        _ => true
    };

    public void Log(LogLevel level, string message, Exception? ex = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        var evt = level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Info => LogEventLevel.Information,
            LogLevel.Warn => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Fatal => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

        _logger
            .ForContext("Caller", $"{Path.GetFileName(file)}:{caller}:{line}")
            .Write(evt, ex, message);
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message, Exception? ex = null) => Log(LogLevel.Warn, message, ex);
    public void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
    public void Fatal(string message, Exception? ex = null) => Log(LogLevel.Fatal, message, ex);
}

/// <summary>
/// Serilog 日志工厂实现。
/// </summary>
public class SerilogLoggerFactory : IAppLoggerFactory
{
    public IAppLogger CreateLogger(string category) => new SerilogLogger(category);
    public IAppLogger CreateLogger<T>() => new SerilogLogger(typeof(T).FullName ?? typeof(T).Name);
}
