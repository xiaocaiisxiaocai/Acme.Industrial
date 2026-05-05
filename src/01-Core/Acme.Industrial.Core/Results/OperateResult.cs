namespace Acme.Industrial.Core.Results;

/// <summary>
/// 操作返回结果（无返回值）。
/// </summary>
public class OperateResult
{
    /// <summary>是否成功。</summary>
    public bool IsSuccess { get; set; }

    /// <summary>错误码（成功时为 0）。</summary>
    public int ErrorCode { get; set; }

    /// <summary>错误消息（成功时为 string.Empty）。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>异常对象（仅在失败且来源是异常时填充）。</summary>
    public Exception? Exception { get; set; }

    /// <summary>耗时（毫秒）。</summary>
    public long ElapsedMilliseconds { get; set; }

    public OperateResult() { }

    public OperateResult(int errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message;
        IsSuccess = errorCode == 0;
    }

    // 工厂方法
    public static OperateResult Ok() =>
        new() { IsSuccess = true, ErrorCode = 0, Message = string.Empty };

    public static OperateResult Fail(int errorCode, string message) =>
        new() { IsSuccess = false, ErrorCode = errorCode, Message = message };

    public static OperateResult Fail(string message) =>
        Fail(-1, message);

    public static OperateResult Fail(Exception ex) =>
        new() { IsSuccess = false, ErrorCode = -1, Message = ex.Message, Exception = ex };

    // 类型转换
    public static OperateResult<T> Ok<T>(T content) =>
        new() { IsSuccess = true, ErrorCode = 0, Content = content };

    public static OperateResult<T> Fail<T>(int errorCode, string message) =>
        new() { IsSuccess = false, ErrorCode = errorCode, Message = message };

    public static OperateResult<T> Fail<T>(OperateResult source) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = source.ErrorCode,
            Message = source.Message,
            Exception = source.Exception
        };

    public override string ToString() =>
        IsSuccess
            ? $"[OK] (Elapsed: {ElapsedMilliseconds}ms)"
            : $"[FAIL] Code={ErrorCode}, Msg={Message}";

    public OperateResult<T> ToTyped<T>() => IsSuccess && Content is T typed
        ? OperateResult.Ok(typed)
        : OperateResult.Fail<T>(this);

    public object? Content { get; set; }
}
