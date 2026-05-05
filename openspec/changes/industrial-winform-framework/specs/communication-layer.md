# Communication Layer - 通讯协议层规格

## Overview

通讯协议层提供统一的设备驱动接口，让上层业务无需关心底层是 Modbus 还是 S7。

## Requirements

### R-1: 数据类型

```csharp
public enum DataType
{
    Bool, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
    Float, Double, String, ByteArray
}
```

### R-2: 字节序

```csharp
public enum EndianFormat
{
    BigEndian,        // ABCD - 大端
    LittleEndian,    // DCBA - 小端
    BigEndianSwap,    // BADC - 字节交换大端
    LittleEndianSwap  // CDAB - 字节交换小端
}
```

### R-3: 点位模型

#### R-3.1 Tag (点位定义)

```csharp
public class Tag
{
    public string Name { get; init; }       // "Reactor1.Temperature"
    public string Address { get; init; }     // "40001", "DB1.DBD0"
    public DataType DataType { get; init; }
    public int ScanRate { get; init; }       // 采集周期 ms
    public double Scale { get; init; } = 1.0;
    public double Offset { get; init; } = 0.0;
    public double DeadBand { get; init; }    // 变化死区
    public string? Unit { get; init; }      // "℃", "bar"
}
```

#### R-3.2 TagValue (实时值)

```csharp
public enum TagQuality { Good, Bad, Uncertain }

public class TagValue
{
    public string TagName { get; init; }
    public object? Value { get; init; }
    public TagQuality Quality { get; init; }
    public DateTime Timestamp { get; init; }
    public byte[]? RawBytes { get; init; }
}
```

### R-4: 连接状态

```csharp
public enum ConnectionState
{
    Disconnected, Connecting, Connected, Reconnecting, Faulted
}
```

### R-5: IDeviceDriver 接口

```csharp
public interface IDeviceDriver : IAsyncDisposable
{
    string DeviceId { get; }
    string Protocol { get; }
    ConnectionState State { get; }
    ConnectionOptions Options { get; }
    CommunicationStatistics Statistics { get; }

    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    // 连接
    Task<OperateResult> ConnectAsync(CancellationToken ct = default);
    Task<OperateResult> DisconnectAsync(CancellationToken ct = default);
    Task<OperateResult> PingAsync(CancellationToken ct = default);

    // 单点读写
    Task<OperateResult<TagValue>> ReadAsync(Tag tag, CancellationToken ct = default);
    Task<OperateResult<T>> ReadAsync<T>(string address, CancellationToken ct = default);
    Task<OperateResult> WriteAsync(Tag tag, object value, CancellationToken ct = default);
    Task<OperateResult> WriteAsync<T>(string address, T value, CancellationToken ct = default);

    // 批量读写
    Task<OperateResult<IReadOnlyList<TagValue>>> ReadBatchAsync(
        IEnumerable<Tag> tags, CancellationToken ct = default);
    Task<OperateResult> WriteBatchAsync(
        IEnumerable<KeyValuePair<Tag, object>> writes, CancellationToken ct = default);

    // 原始字节
    Task<OperateResult<byte[]>> ReadRawAsync(string address, ushort length,
        CancellationToken ct = default);
    Task<OperateResult> WriteRawAsync(string address, byte[] data,
        CancellationToken ct = default);
}
```

### R-6: ITagSubscriber 订阅接口

```csharp
public interface ITagSubscriber : IDisposable
{
    IDisposable Subscribe(IEnumerable<Tag> tags, Action<TagValue> onValueChanged);
    void UnsubscribeAll();
}
```

### R-7: IDeviceManager 设备管理器

```csharp
public interface IDeviceManager : IAsyncDisposable
{
    Task<OperateResult> AddDeviceAsync(ConnectionOptions options, CancellationToken ct = default);
    Task<OperateResult> RemoveDeviceAsync(string deviceId, CancellationToken ct = default);
    IDeviceDriver? GetDevice(string deviceId);
    IReadOnlyList<IDeviceDriver> GetAllDevices();
    Task<OperateResult> ConnectAllAsync(CancellationToken ct = default);
    Task<OperateResult> DisconnectAllAsync(CancellationToken ct = default);
    event EventHandler<DeviceStateChangedEventArgs>? AnyDeviceStateChanged;
}
```

### R-8: IDriverFactory 驱动工厂

```csharp
public interface IDriverFactory
{
    void Register(string protocol, Func<ConnectionOptions, IDeviceDriver> factory);
    IDeviceDriver Create(ConnectionOptions options);
    IReadOnlyList<string> GetSupportedProtocols();
}
```

### R-9: DeviceDriverBase 抽象基类

封装通用逻辑：
- 连接状态管理
- 重试机制
- 统计信息
- 心跳检测
- 断线重连

子类只需实现：
- `ConnectCoreAsync` - 协议连接
- `ReadRawCoreAsync` - 原始读取
- `WriteRawCoreAsync` - 原始写入
- `PingCoreAsync` - 心跳检测

### R-10: Mock 驱动

```csharp
public class MockDeviceDriver : DeviceDriverBase
{
    public int SimulatedDelayMs { get; set; } = 10;
    public double ErrorRate { get; set; } = 0;
}
```

用于无设备开发、单元测试、CI 环境。

---

## Acceptance Criteria

- [ ] IDeviceDriver 支持所有基本读写操作
- [ ] 批量读写支持地址合并优化
- [ ] ITagSubscriber 支持死区过滤
- [ ] DeviceDriverBase 封装重试、统计、心跳
- [ ] Mock 驱动可用于测试
- [ ] 支持至少 3 种协议 (ModbusTCP, SiemensS7, MitsubishiMC)
- [ ] 所有操作支持 CancellationToken
- [ ] 统计信息包含读写次数、成功率、延时

---

## Dependencies

- `core-abstractions`
