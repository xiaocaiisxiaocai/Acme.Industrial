namespace Acme.Industrial.Core.Results;

/// <summary>
/// 操作返回结果（带返回值）。
/// </summary>
/// <typeparam name="T">返回类型。</typeparam>
public class OperateResult<T> : OperateResult
{
    /// <summary>返回内容。</summary>
    public new T? Content { get; set; }

    public OperateResult() { }

    public OperateResult(T content)
    {
        IsSuccess = true;
        ErrorCode = 0;
        Content = content;
    }

    // 隐式转换
    public static implicit operator OperateResult<T>(T value) => OperateResult.Ok(value);
    public static implicit operator T?(OperateResult<T> result) => result.Content;
}
