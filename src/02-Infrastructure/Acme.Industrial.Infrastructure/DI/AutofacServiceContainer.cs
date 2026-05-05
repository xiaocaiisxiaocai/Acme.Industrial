using Autofac;
using Acme.Industrial.Core.Abstractions;

namespace Acme.Industrial.Infrastructure.DI;

/// <summary>
/// Autofac DI 容器包装器。
/// </summary>
public class AutofacServiceContainer : IServiceRegistry, IServiceResolver
{
    private readonly ContainerBuilder _builder = new();
    private readonly List<Type> _registeredTypes = new();
    private IContainer? _container;
    private bool _isLocked;
    private readonly List<Action<ContainerBuilder>> _configurationActions = new();

    public void Register<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService
    {
        CheckNotLocked();
        var rb = _builder.RegisterType<TImplementation>().As<TService>();
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                rb.SingleInstance();
                break;
            case ServiceLifetime.Scoped:
                rb.InstancePerLifetimeScope();
                break;
            case ServiceLifetime.Transient:
                rb.InstancePerDependency();
                break;
        }
        _registeredTypes.Add(typeof(TService));
    }

    public void Register<TService>(TService instance) where TService : class
    {
        CheckNotLocked();
        _builder.RegisterInstance(instance).As<TService>();
        _registeredTypes.Add(typeof(TService));
    }

    public void Register(Type service, Type implementation, ServiceLifetime lifetime)
    {
        CheckNotLocked();
        var rb = _builder.RegisterType(implementation).As(service);
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                rb.SingleInstance();
                break;
            case ServiceLifetime.Scoped:
                rb.InstancePerLifetimeScope();
                break;
            case ServiceLifetime.Transient:
                rb.InstancePerDependency();
                break;
        }
        _registeredTypes.Add(service);
    }

    public void RegisterGeneric(Type service, Type implementation, ServiceLifetime lifetime)
    {
        CheckNotLocked();
        var rb = _builder.RegisterGeneric(implementation).As(service);
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                rb.SingleInstance();
                break;
            case ServiceLifetime.Scoped:
                rb.InstancePerLifetimeScope();
                break;
            case ServiceLifetime.Transient:
                rb.InstancePerDependency();
                break;
        }
        _registeredTypes.Add(service);
    }

    public bool IsRegistered<TService>() where TService : class
    {
        EnsureContainer();
        return _container!.IsRegistered<TService>();
    }

    public bool IsRegistered(Type service)
    {
        EnsureContainer();
        return _container!.IsRegistered(service);
    }

    public T Resolve<T>() where T : class
    {
        EnsureContainer();
        return _container!.Resolve<T>();
    }

    public T? TryResolve<T>() where T : class
    {
        EnsureContainer();
        return _container!.TryResolve<T>(out var result) ? result : default;
    }

    public IEnumerable<T> ResolveAll<T>() where T : class
    {
        EnsureContainer();
        return _container!.Resolve<IEnumerable<T>>();
    }

    public IServiceResolver CreateScope()
    {
        EnsureContainer();
        return new ScopeResolver(_container!.BeginLifetimeScope());
    }

    public void RegisterFromModule(IModule module)
    {
        module.RegisterServices(this);
    }

    public void Configure(Action<ContainerBuilder> action)
    {
        _configurationActions.Add(action);
    }

    public void Build()
    {
        if (_container != null) return;

        foreach (var action in _configurationActions)
        {
            action(_builder);
        }

        _container = _builder.Build();
        _isLocked = true;
    }

    private void CheckNotLocked()
    {
        if (_isLocked)
            throw new InvalidOperationException("容器已构建，禁止注册");
    }

    private void EnsureContainer()
    {
        if (_container == null)
            Build();
    }

    public void Dispose()
    {
        _container?.Dispose();
    }

    private class ScopeResolver : IServiceResolver
    {
        private readonly ILifetimeScope _scope;

        public ScopeResolver(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public T Resolve<T>() where T : class => _scope.Resolve<T>();

        public T? TryResolve<T>() where T : class =>
            _scope.TryResolve(out T? result) ? result : default;

        public IEnumerable<T> ResolveAll<T>() where T : class => _scope.Resolve<IEnumerable<T>>();

        public IServiceResolver CreateScope() => new ScopeResolver(_scope.BeginLifetimeScope());

        public void Dispose() => _scope.Dispose();
    }
}
