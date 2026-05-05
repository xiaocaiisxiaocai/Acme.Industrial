namespace Acme.Industrial.Core.Exceptions;

/// <summary>
/// 框架异常基类。
/// </summary>
public abstract class FrameworkException : Exception
{
    /// <summary>
    /// 错误码。
    /// </summary>
    public int ErrorCode { get; }

    protected FrameworkException(int code, string message, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = code;
    }
}

/// <summary>
/// 业务异常。
/// </summary>
public class BusinessException : FrameworkException
{
    public BusinessException(int code, string message, Exception? inner = null)
        : base(code, message, inner) { }

    public BusinessException(string message)
        : base(Results.ErrorCode.BizValidationFailed, message) { }
}

/// <summary>
/// 通讯异常。
/// </summary>
public class CommunicationException : FrameworkException
{
    /// <summary>
    /// 设备 ID。
    /// </summary>
    public string? DeviceId { get; }

    public CommunicationException(int code, string message, string? deviceId = null,
        Exception? inner = null)
        : base(code, message, inner)
    {
        DeviceId = deviceId;
    }

    public CommunicationException(string message, string? deviceId = null)
        : base(Results.ErrorCode.CommReadFailed, message)
    {
        DeviceId = deviceId;
    }
}

/// <summary>
/// 配置异常。
/// </summary>
public class ConfigurationException : FrameworkException
{
    public ConfigurationException(string message, Exception? inner = null)
        : base(Results.ErrorCode.ConfigInvalid, message, inner) { }
}

/// <summary>
/// 授权异常。
/// </summary>
public class AuthorizationException : FrameworkException
{
    public AuthorizationException(string message)
        : base(Results.ErrorCode.AuthForbidden, message) { }
}

/// <summary>
/// 脚本异常。
/// </summary>
public class ScriptException : FrameworkException
{
    public string? ScriptId { get; }

    public ScriptException(int code, string message, string? scriptId = null, Exception? inner = null)
        : base(code, message, inner)
    {
        ScriptId = scriptId;
    }

    public ScriptException(string message, string? scriptId = null)
        : base(Results.ErrorCode.ScriptRuntimeError, message)
    {
        ScriptId = scriptId;
    }
}
