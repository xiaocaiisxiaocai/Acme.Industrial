namespace Acme.Industrial.Core.Abstractions;

/// <summary>
/// 服务生命周期。
/// </summary>
public enum ServiceLifetime
{
    /// <summary>单例 - 整个应用生命周期内只有一个实例。</summary>
    Singleton,

    /// <summary>作用域 - 每个作用域创建一个实例。</summary>
    Scoped,

    /// <summary>瞬态 - 每次请求创建一个新实例。</summary>
    Transient
}

/// <summary>
/// 服务注册表接口。
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// 注册服务。
    /// </summary>
    void Register<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// 注册实例（单例）。
    /// </summary>
    void Register<TService>(TService instance) where TService : class;

    /// <summary>
    /// 注册服务（使用类型）。
    /// </summary>
    void Register(Type service, Type implementation, ServiceLifetime lifetime);

    /// <summary>
    /// 注册泛型服务。
    /// </summary>
    void RegisterGeneric(Type service, Type implementation, ServiceLifetime lifetime);

    /// <summary>
    /// 检查服务是否已注册。
    /// </summary>
    bool IsRegistered<TService>() where TService : class;

    /// <summary>
    /// 检查服务是否已注册。
    /// </summary>
    bool IsRegistered(Type service);
}

/// <summary>
/// 服务解析器接口。
/// </summary>
public interface IServiceResolver : IDisposable
{
    /// <summary>
    /// 解析服务。
    /// </summary>
    T Resolve<T>() where T : class;

    /// <summary>
    /// 尝试解析服务。
    /// </summary>
    T? TryResolve<T>() where T : class;

    /// <summary>
    /// 解析所有实现。
    /// </summary>
    IEnumerable<T> ResolveAll<T>() where T : class;

    /// <summary>
    /// 创建新的作用域。
    /// </summary>
    IServiceResolver CreateScope();
}
