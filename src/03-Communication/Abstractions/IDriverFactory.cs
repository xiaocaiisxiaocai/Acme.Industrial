using Acme.Industrial.Communication.Abstractions.Models;

namespace Acme.Industrial.Communication.Abstractions;

/// <summary>
/// 驱动工厂接口 - 用于创建设备驱动实例。
/// </summary>
public interface IDriverFactory
{
    /// <summary>
    /// 注册协议驱动。
    /// </summary>
    /// <param name="protocol">协议名称，如 "ModbusTcp"、"SiemensS7"、"MitsubishiMc"。</param>
    /// <param name="factory">驱动创建工厂。</param>
    void Register(string protocol, Func<ConnectionOptions, IDeviceDriver> factory);

    /// <summary>
    /// 创建驱动实例。
    /// </summary>
    /// <param name="options">连接选项。</param>
    /// <returns>设备驱动实例。</returns>
    /// <exception cref="NotSupportedException">当协议不支持时抛出。</exception>
    IDeviceDriver Create(ConnectionOptions options);

    /// <summary>
    /// 获取支持的协议列表。
    /// </summary>
    /// <returns>协议名称列表。</returns>
    IReadOnlyList<string> GetSupportedProtocols();

    /// <summary>
    /// 检查协议是否支持。
    /// </summary>
    /// <param name="protocol">协议名称。</param>
    /// <returns>是否支持。</returns>
    bool IsProtocolSupported(string protocol);
}
