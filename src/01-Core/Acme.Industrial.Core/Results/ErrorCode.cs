namespace Acme.Industrial.Core.Results;

/// <summary>
/// 错误码定义。
/// 错误码分段管理，便于定位来源。
/// </summary>
public static class ErrorCode
{
    // 0: 成功
    public const int Success = 0;

    // -1 ~ -99: 通用错误
    public const int Unknown = -1;
    public const int InvalidArgument = -2;
    public const int Timeout = -3;
    public const int NotImplemented = -4;
    public const int Cancelled = -5;
    public const int InvalidOperation = -6;
    public const int NullReference = -7;
    public const int ArgumentOutOfRange = -8;

    // 1xxx: 通讯错误
    public const int CommNotConnected = 1001;
    public const int CommConnectFailed = 1002;
    public const int CommReadFailed = 1003;
    public const int CommWriteFailed = 1004;
    public const int CommCrcError = 1005;
    public const int CommAddressInvalid = 1006;
    public const int CommProtocolError = 1007;
    public const int CommTimeout = 1008;

    // 2xxx: 数据库错误
    public const int DbConnectFailed = 2001;
    public const int DbExecuteFailed = 2002;
    public const int DbRecordNotFound = 2003;
    public const int DbDuplicateKey = 2004;
    public const int DbConstraintViolation = 2005;

    // 3xxx: 权限/认证错误
    public const int AuthUnauthorized = 3001;
    public const int AuthForbidden = 3002;
    public const int AuthTokenExpired = 3003;
    public const int AuthInvalidCredentials = 3004;
    public const int AuthAccountLocked = 3005;

    // 4xxx: 业务错误
    public const int BizValidationFailed = 4001;
    public const int BizConflict = 4002;
    public const int BizNotFound = 4003;
    public const int BizStateInvalid = 4004;

    // 5xxx: 配置错误
    public const int ConfigNotFound = 5001;
    public const int ConfigInvalid = 5002;
    public const int ConfigReadOnly = 5003;

    // 6xxx: 脚本错误
    public const int ScriptCompileError = 6001;
    public const int ScriptRuntimeError = 6002;
    public const int ScriptTimeout = 6003;
    public const int ScriptSecurityViolation = 6004;
    public const int ScriptNotFound = 6005;

    // 7xxx: HMI/UI 错误
    public const int HmiProjectLoadFailed = 7001;
    public const int HmiElementNotFound = 7002;
    public const int HmiSerializationError = 7003;
    public const int HmiBindingFailed = 7004;

    // 8xxx: 运动控制错误
    public const int MotionControllerNotConnected = 8001;
    public const int MotionAxisNotFound = 8002;
    public const int MotionCommandFailed = 8003;
    public const int MotionFollowError = 8004;
    public const int MotionLimitTriggered = 8005;
    public const int MotionHomingFailed = 8006;

    // 9xxx: 视觉系统错误
    public const int VisionCameraNotConnected = 9001;
    public const int VisionGrabFailed = 9002;
    public const int VisionToolNotFound = 9003;
    public const int VisionToolExecutionFailed = 9004;
    public const int VisionCalibrationFailed = 9005;

    /// <summary>
    /// 获取错误码对应的默认消息。
    /// </summary>
    public static string GetMessage(int errorCode)
    {
        return errorCode switch
        {
            Success => "操作成功",
            Unknown => "未知错误",
            InvalidArgument => "参数无效",
            Timeout => "操作超时",
            NotImplemented => "功能未实现",
            Cancelled => "操作已取消",
            InvalidOperation => "操作无效",
            NullReference => "空引用",
            ArgumentOutOfRange => "参数超出范围",
            CommNotConnected => "设备未连接",
            CommConnectFailed => "连接失败",
            CommReadFailed => "读取失败",
            CommWriteFailed => "写入失败",
            CommCrcError => "校验错误",
            CommAddressInvalid => "地址无效",
            CommProtocolError => "协议错误",
            CommTimeout => "通讯超时",
            DbConnectFailed => "数据库连接失败",
            DbExecuteFailed => "数据库执行失败",
            DbRecordNotFound => "记录不存在",
            DbDuplicateKey => "重复键",
            DbConstraintViolation => "约束冲突",
            AuthUnauthorized => "未授权",
            AuthForbidden => "禁止访问",
            AuthTokenExpired => "令牌过期",
            AuthInvalidCredentials => "凭证无效",
            AuthAccountLocked => "账户已锁定",
            BizValidationFailed => "业务验证失败",
            BizConflict => "业务冲突",
            BizNotFound => "业务对象不存在",
            BizStateInvalid => "状态无效",
            ConfigNotFound => "配置项不存在",
            ConfigInvalid => "配置无效",
            ConfigReadOnly => "配置只读",
            ScriptCompileError => "脚本编译错误",
            ScriptRuntimeError => "脚本运行时错误",
            ScriptTimeout => "脚本执行超时",
            ScriptSecurityViolation => "脚本安全违规",
            ScriptNotFound => "脚本不存在",
            HmiProjectLoadFailed => "HMI项目加载失败",
            HmiElementNotFound => "HMI元素不存在",
            HmiSerializationError => "HMI序列化错误",
            HmiBindingFailed => "HMI绑定失败",
            MotionControllerNotConnected => "运动控制器未连接",
            MotionAxisNotFound => "运动轴不存在",
            MotionCommandFailed => "运动命令执行失败",
            MotionFollowError => "跟随误差超限",
            MotionLimitTriggered => "限位触发",
            MotionHomingFailed => "回零失败",
            VisionCameraNotConnected => "相机未连接",
            VisionGrabFailed => "图像采集失败",
            VisionToolNotFound => "视觉工具不存在",
            VisionToolExecutionFailed => "视觉工具执行失败",
            VisionCalibrationFailed => "标定失败",
            _ => $"未知错误码: {errorCode}"
        };
    }
}
