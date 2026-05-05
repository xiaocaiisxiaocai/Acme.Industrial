using System.Reflection;
using Acme.Industrial.Core.Abstractions;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Core.Bootstrap;

public class ModuleLoadPolicyAttribute : Attribute
{
    public ModuleLoadPolicy Policy { get; }

    public ModuleLoadPolicyAttribute(ModuleLoadPolicy policy)
    {
        Policy = policy;
    }
}

public class ModulePriorityAttribute : Attribute
{
    public int Priority { get; }

    public ModulePriorityAttribute(int priority)
    {
        Priority = priority;
    }
}

/// <summary>
/// 应用程序启动引导器。
/// </summary>
public class ApplicationBootstrap
{
    private readonly IModuleRegistry _registry;
    private readonly IModuleManager _manager;
    private readonly IAppLogger _logger;
    private readonly IServiceRegistry _serviceRegistry;
    private readonly IServiceResolver _serviceResolver;

    public ApplicationBootstrap(
        IModuleRegistry registry,
        IModuleManager manager,
        IAppLoggerFactory loggerFactory,
        IServiceRegistry serviceRegistry,
        IServiceResolver serviceResolver)
    {
        _registry = registry;
        _manager = manager;
        _logger = loggerFactory.CreateLogger<ApplicationBootstrap>();
        _serviceRegistry = serviceRegistry;
        _serviceResolver = serviceResolver;
    }

    /// <summary>
    /// 启动应用程序。
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.Info("=== 应用启动 ===");

        // 1. 扫描模块
        var modules = ScanModules();
        _registry.RegisterRange(modules);
        _logger.Info($"发现 {modules.Count} 个模块");

        // 2. 拓扑排序（按依赖关系排序）
        var sorted = TopologicalSort(modules);

        // 3. 加载启动必须模块（Eager）
        var context = new ModuleLoadContext
        {
            Registry = _registry,
            Services = _serviceResolver,
            LoggerFactory = _serviceResolver.Resolve<IAppLoggerFactory>()!,
            IsStartup = true,
            LoadedCount = 0
        };

        foreach (var metadata in sorted.Where(m => m.LoadPolicy == ModuleLoadPolicy.Eager))
        {
            _logger.Info($"加载模块: {metadata.Name}");
            var result = await _manager.LoadModuleAsync(metadata.Id, ct);
            if (!result.IsSuccess)
            {
                _logger.Error($"模块加载失败: {metadata.Name} - {result.Message}");
            }
        }

        // 4. 后台预加载（Preload）
        _ = _manager.PreloadAllAsync(ct);

        _logger.Info("=== 应用启动完成 ===");
    }

    /// <summary>
    /// 停止应用程序。
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        _logger.Info("=== 应用停止 ===");
        await _manager.DisposeAsync();
        _logger.Info("=== 应用已停止 ===");
    }

    private List<ModuleMetadata> ScanModules()
    {
        var modules = new List<ModuleMetadata>();
        var searchPaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins")
        };

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath)) continue;

            foreach (var file in Directory.GetFiles(searchPath, "*.dll"))
            {
                try
                {
                    var metadata = ExtractMetadata(file);
                    if (metadata != null)
                        modules.Add(metadata);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"扫描模块失败: {file}", ex);
                }
            }
        }

        return modules;
    }

    private ModuleMetadata? ExtractMetadata(string dllPath)
    {
        Assembly? assembly;
        try
        {
            assembly = Assembly.LoadFrom(dllPath);
        }
        catch
        {
            return null;
        }

        var entryType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface);

        if (entryType == null) return null;

        var module = (IModule?)Activator.CreateInstance(entryType);
        if (module == null) return null;

        var loadPolicy = ModuleLoadPolicy.Lazy;
        var policyAttr = entryType.GetCustomAttribute<ModuleLoadPolicyAttribute>();
        if (policyAttr != null)
            loadPolicy = policyAttr.Policy;

        // 从特性读取优先级
        var priority = 100;
        var priorityAttr = entryType.GetCustomAttribute<ModulePriorityAttribute>();
        if (priorityAttr != null)
            priority = priorityAttr.Priority;

        return new ModuleMetadata
        {
            Id = module.Id,
            Name = module.Name,
            Version = module.Version,
            EntryType = entryType.FullName ?? string.Empty,
            AssemblyPath = dllPath,
            Dependencies = module.Dependencies,
            LoadPolicy = loadPolicy
        };
    }

    private List<ModuleMetadata> TopologicalSort(List<ModuleMetadata> modules)
    {
        var result = new List<ModuleMetadata>();
        var visited = new HashSet<string>();
        var moduleDict = modules.ToDictionary(m => m.Id);

        void Visit(ModuleMetadata m)
        {
            if (visited.Contains(m.Id)) return;
            visited.Add(m.Id);

            foreach (var depId in m.Dependencies)
            {
                if (moduleDict.TryGetValue(depId, out var dep))
                    Visit(dep);
            }

            result.Add(m);
        }

        foreach (var m in modules)
            Visit(m);

        return result;
    }
}
