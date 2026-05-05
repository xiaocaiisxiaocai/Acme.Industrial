using Acme.Industrial.Communication.Abstractions.Models;
using Acme.Industrial.Communication.Abstractions.Enumerations;

namespace Acme.Industrial.Communication.Abstractions;

/// <summary>
/// 连接选项。
/// </summary>
public class ConnectionOptions
{
    /// <summary>
    /// 设备唯一 ID。
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// 设备显示名称。
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// 协议类型，如 "ModbusTcp"、"SiemensS7"。
    /// </summary>
    public string Protocol { get; init; } = string.Empty;

    // 网络相关
    public string? Host { get; init; }
    public int Port { get; init; }

    // 串口相关
    public string? PortName { get; init; }
    public int BaudRate { get; init; } = 9600;
    public int DataBits { get; init; } = 8;
    public string? Parity { get; init; } = "None";
    public string? StopBits { get; init; } = "One";

    // 协议相关
    public byte SlaveAddress { get; init; } = 1;
    public int Rack { get; init; }
    public int Slot { get; init; }

    // 超时和重试
    public int ConnectTimeoutMs { get; init; } = 3000;
    public int ReadTimeoutMs { get; init; } = 2000;
    public int WriteTimeoutMs { get; init; } = 2000;
    public int RetryCount { get; init; } = 3;
    public int RetryIntervalMs { get; init; } = 500;
    public int HeartbeatIntervalMs { get; init; } = 5000;

    // 字节序
    public EndianFormat Endian { get; init; } = EndianFormat.BigEndian;

    /// <summary>
    /// 协议特定的扩展参数。
    /// </summary>
    public IReadOnlyDictionary<string, string> Extras { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// 通讯统计。
/// </summary>
public class CommunicationStatistics
{
    public long TotalReads { get; set; }
    public long SuccessReads { get; set; }
    public long FailedReads { get; set; }
    public long TotalWrites { get; set; }
    public long SuccessWrites { get; set; }
    public long FailedWrites { get; set; }
    public long TotalBytesReceived { get; set; }
    public long TotalBytesSent { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public string? LastErrorMessage { get; set; }
    public double AverageReadLatencyMs { get; set; }
    public DateTime ConnectedSince { get; set; }

    public double ReadSuccessRate => TotalReads == 0 ? 0 : (double)SuccessReads / TotalReads;
}

/// <summary>
/// 连接状态变更事件参数。
/// </summary>
public class ConnectionStateChangedEventArgs
{
    public ConnectionState OldState { get; init; }
    public ConnectionState NewState { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// 通讯错误事件参数。
/// </summary>
public class CommunicationErrorEventArgs
{
    public int ErrorCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}

/// <summary>
/// 设备状态变更事件参数。
/// </summary>
public class DeviceStateChangedEventArgs : EventArgs
{
    public string DeviceId { get; init; } = string.Empty;
    public ConnectionState OldState { get; init; }
    public ConnectionState NewState { get; init; }
    public string? Message { get; init; }
}
