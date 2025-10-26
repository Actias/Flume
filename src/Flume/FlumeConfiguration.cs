using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Flume.Entities;
using Flume.NotificationPublishers;
using Flume.Pipelines;
using Flume.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Flume;

/// <summary>
/// Performance optimization strategy for Flume caching and compilation
/// </summary>
public enum PerformanceStrategy
{
    /// <summary>
    /// Optimized for MediatR compatibility - minimal caching, similar performance characteristics to MediatR
    /// </summary>
    MediatRCompatible,
    
    /// <summary>
    /// Balanced approach - moderate caching with good performance and memory usage
    /// </summary>
    Balanced,
    
    /// <summary>
    /// Maximum performance - aggressive caching, pre-compilation, and optimization
    /// </summary>
    MaximumPerformance
}

/// <summary>
/// Fine-grained cache configuration options for customizing caching behavior
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Maximum size for request handler cache. Default is 1000.
    /// </summary>
    public int RequestHandlerCacheSize { get; set; } = 1000;

    /// <summary>
    /// Maximum size for notification handler cache. Default is 1000.
    /// </summary>
    public int NotificationHandlerCacheSize { get; set; } = 1000;

    /// <summary>
    /// Maximum size for stream request handler cache. Default is 1000.
    /// </summary>
    public int StreamRequestHandlerCacheSize { get; set; } = 1000;

    /// <summary>
    /// Maximum size for type information cache. Default is 1000.
    /// </summary>
    public int TypeInfoCacheSize { get; set; } = 1000;

    /// <summary>
    /// Maximum size for service resolution cache. Default is 1000.
    /// </summary>
    public int ServiceResolutionCacheSize { get; set; } = 1000;

    /// <summary>
    /// Maximum size for compiled pipeline cache. Default is 1000.
    /// </summary>
    public int CompiledPipelineCacheSize { get; set; } = 1000;

    /// <summary>
    /// Enable cache warming during startup. Default is false.
    /// </summary>
    public bool EnableCacheWarming { get; set; }

    /// <summary>
    /// Cache eviction policy. Default is LRU (Least Recently Used).
    /// </summary>
    public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LRU;

    /// <summary>
    /// Time-to-live for cached items in minutes. 0 means no expiration. Default is 0 (no expiration).
    /// </summary>
    public int CacheTtlMinutes { get; set; }

    /// <summary>
    /// Enable cache statistics collection. Default is false.
    /// </summary>
    public bool EnableCacheStatistics { get; set; }

    /// <summary>
    /// Create a MediatR-compatible cache configuration
    /// </summary>
    public static CacheConfiguration MediatRCompatible()
    {
        return new CacheConfiguration
        {
            RequestHandlerCacheSize = 100,
            NotificationHandlerCacheSize = 100,
            StreamRequestHandlerCacheSize = 100,
            TypeInfoCacheSize = 100,
            ServiceResolutionCacheSize = 100,
            CompiledPipelineCacheSize = 100,
            EnableCacheWarming = false,
            EvictionPolicy = CacheEvictionPolicy.LRU,
            CacheTtlMinutes = 0,
            EnableCacheStatistics = false
        };
    }

    /// <summary>
    /// Create a balanced cache configuration (default)
    /// </summary>
    public static CacheConfiguration Balanced()
    {
        return new CacheConfiguration
        {
            RequestHandlerCacheSize = 1000,
            NotificationHandlerCacheSize = 1000,
            StreamRequestHandlerCacheSize = 1000,
            TypeInfoCacheSize = 1000,
            ServiceResolutionCacheSize = 1000,
            CompiledPipelineCacheSize = 1000,
            EnableCacheWarming = false,
            EvictionPolicy = CacheEvictionPolicy.LRU,
            CacheTtlMinutes = 0,
            EnableCacheStatistics = false
        };
    }

    /// <summary>
    /// Create a maximum performance cache configuration
    /// </summary>
    public static CacheConfiguration MaximumPerformance()
    {
        return new CacheConfiguration
        {
            RequestHandlerCacheSize = 10000,
            NotificationHandlerCacheSize = 10000,
            StreamRequestHandlerCacheSize = 10000,
            TypeInfoCacheSize = 10000,
            ServiceResolutionCacheSize = 10000,
            CompiledPipelineCacheSize = 10000,
            EnableCacheWarming = true,
            EvictionPolicy = CacheEvictionPolicy.LRU,
            CacheTtlMinutes = 0,
            EnableCacheStatistics = true
        };
    }
}

/// <summary>
/// Cache eviction policy options
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>
    /// Least Recently Used - evicts the least recently accessed items first
    /// </summary>
    LRU,
    
    /// <summary>
    /// Least Frequently Used - evicts the least frequently accessed items first
    /// </summary>
    LFU,
    
    /// <summary>
    /// First In First Out - evicts the oldest items first
    /// </summary>
    FIFO,
    
    /// <summary>
    /// Random eviction - evicts items randomly
    /// </summary>
    Random
}

/// <summary>
/// 
/// </summary>
public enum RequestExceptionActionProcessorStrategy
{
    /// <summary>
    /// 
    /// </summary>
    ApplyForUnhandledExceptions,
    /// <summary>
    /// 
    /// </summary>
    ApplyForAllExceptions
}

/// <summary>
/// 
/// </summary>
public class FlumeConfiguration
{
    /// <summary>
    /// Optional filter for types to register. Default value is a function returning true.
    /// </summary>
    public Func<Type, bool> TypeEvaluator { get; set; } = _ => true;

    /// <summary>
    /// Mediator implementation type to register. Default is <see cref="Mediator"/>
    /// </summary>
    public Type MediatorImplementationType { get; set; } = typeof(Mediator);

    /// <summary>
    /// Strategy for publishing notifications. Defaults to <see cref="ForeachAwaitPublisher"/>
    /// </summary>
    public INotificationPublisher NotificationPublisher { get; set; } = new ForeachAwaitPublisher();

    /// <summary>
    /// Type of notification publisher strategy to register. If set, overrides <see cref="NotificationPublisher"/>
    /// </summary>
    public Type? NotificationPublisherType { get; set; }

    /// <summary>
    /// Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Transient"/>
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Request exception action processor strategy. Default value is <see cref="RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions"/>
    /// </summary>
    public RequestExceptionActionProcessorStrategy RequestExceptionActionProcessorStrategy { get; set; }
        = RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions;

    internal List<Assembly> AssembliesToRegister { get; } = [];

    /// <summary>
    /// List of behaviors to register in specific order
    /// </summary>
    public Collection<ServiceDescriptor> BehaviorsToRegister { get; } = [];

    /// <summary>
    /// List of stream behaviors to register in specific order
    /// </summary>
    public Collection<ServiceDescriptor> StreamBehaviorsToRegister { get; } = [];

    /// <summary>
    /// List of request pre-processors to register in specific order
    /// </summary>
    public Collection<ServiceDescriptor> RequestPreProcessorsToRegister { get; } = [];

    /// <summary>
    /// List of request post processors to register in specific order
    /// </summary>
    public Collection<ServiceDescriptor> RequestPostProcessorsToRegister { get; } = [];

    /// <summary>
    /// Automatically register processors during assembly scanning
    /// </summary>
    public bool AutoRegisterRequestProcessors { get; set; }

    /// <summary>
    /// Configure the maximum number of type parameters that a generic request handler can have. To Disable this constraint, set the value to 0.
    /// </summary>
    public int MaxGenericTypeParameters { get; set; } = 10;

    /// <summary>
    /// Configure the maximum number of types that can close a generic request type parameter constraint.  To Disable this constraint, set the value to 0.
    /// </summary>
    public int MaxTypesClosing { get; set; } = 100;

    /// <summary>
    /// Configure the Maximum Amount of Generic RequestHandler Types Flume will try to register.  To Disable this constraint, set the value to 0.
    /// </summary>
    public int MaxGenericTypeRegistrations { get; set; } = 125000;

    /// <summary>
    /// Configure the Timeout in Milliseconds that the GenericHandler Registration Process will exit with error.  To Disable this constraint, set the value to 0.
    /// </summary>
    public int RegistrationTimeout { get; set; } = 15000;

    /// <summary>
    /// Flag that controls whether Flume will attempt to register handlers that containing generic type parameters.
    /// </summary>
    public bool RegisterGenericHandlers { get; set; }

    /// <summary>
    /// Flag that controls whether Flume will scan assemblies for handlers when registering services.
    /// </summary>
    public bool ScanForHandlers { get; set; } = true;
    
    /// <summary>
    /// Enable object pooling for handler wrappers. Default is true.
    /// </summary>
    public bool EnableObjectPooling { get; set; } = true;
    
    /// <summary>
    /// Enable pre-compiled pipeline behaviors. Default is true.
    /// </summary>
    public bool EnablePipelineCompilation { get; set; } = true;
    
    /// <summary>
    /// Enable aggressive type caching. Default is true.
    /// </summary>
    public bool EnableTypeCaching { get; set; } = true;
    
    /// <summary>
    /// Maximum cache size for handler wrappers. Default is 1000.
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Performance optimization strategy for caching and compilation.
    /// </summary>
    public PerformanceStrategy PerformanceStrategy { get; set; } = PerformanceStrategy.Balanced;

    /// <summary>
    /// Custom cache configuration options for fine-grained control over caching behavior.
    /// </summary>
    public CacheConfiguration CacheConfiguration { get; set; } = new();

    /// <summary>
    /// Apply performance strategy settings to optimize caching and compilation behavior.
    /// This method automatically configures EnableObjectPooling, EnablePipelineCompilation, 
    /// EnableTypeCaching, and MaxCacheSize based on the selected strategy.
    /// </summary>
    /// <param name="strategy">The performance strategy to apply</param>
    /// <returns>This configuration instance for method chaining</returns>
    public FlumeConfiguration WithPerformanceStrategy(PerformanceStrategy strategy)
    {
        PerformanceStrategy = strategy;
        
        switch (strategy)
        {
            case PerformanceStrategy.MediatRCompatible:
                // Minimal caching for MediatR compatibility
                EnableObjectPooling = false;
                EnablePipelineCompilation = false;
                EnableTypeCaching = false;
                MaxCacheSize = 100;
                CacheConfiguration = CacheConfiguration.MediatRCompatible();
                break;
                
            case PerformanceStrategy.Balanced:
                // Balanced approach - current defaults
                EnableObjectPooling = true;
                EnablePipelineCompilation = true;
                EnableTypeCaching = true;
                MaxCacheSize = 1000;
                CacheConfiguration = CacheConfiguration.Balanced();
                break;
                
            case PerformanceStrategy.MaximumPerformance:
                // Aggressive optimization
                EnableObjectPooling = true;
                EnablePipelineCompilation = true;
                EnableTypeCaching = true;
                MaxCacheSize = 10000;
                CacheConfiguration = CacheConfiguration.MaximumPerformance();
                break;
        }
        
        return this;
    }

    /// <summary>
    /// Configure custom caching options for fine-grained control.
    /// </summary>
    /// <param name="configureCache">Action to configure cache settings</param>
    /// <returns>This configuration instance for method chaining</returns>
    public FlumeConfiguration WithCustomCache(Action<CacheConfiguration> configureCache)
    {
        configureCache(CacheConfiguration);
        return this;
    }

    /// <summary>
    /// Register various handlers from assembly containing given type
    /// </summary>
    /// <typeparam name="T">Type from assembly to scan</typeparam>
    /// <returns>This</returns>
    public FlumeConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssemblyContaining(typeof(T));

    /// <summary>
    /// Register various handlers from assembly containing given type
    /// </summary>
    /// <param name="type">Type from assembly to scan</param>
    /// <returns>This</returns>
    public FlumeConfiguration RegisterServicesFromAssemblyContaining(Type type)
        => RegisterServicesFromAssembly(type.Assembly);

    /// <summary>
    /// Register various handlers from assembly
    /// </summary>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>This</returns>
    public FlumeConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);

        return this;
    }

    /// <summary>
    /// Register various handlers from assemblies
    /// </summary>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>This</returns>
    public FlumeConfiguration RegisterServicesFromAssemblies(
        params Assembly[] assemblies)
    {
        AssembliesToRegister.AddRange(assemblies);

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <typeparam name="TServiceType">Closed behavior interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed behavior type against all <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return AddBehavior(typeof(TImplementationType), serviceLifetime);
    }

    /// <summary>
    /// Register a closed behavior type against all <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IPipelineBehavior<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var implementedBehaviorType in implementedGenericInterfaces)
        {
            BehaviorsToRegister.Add(new(implementedBehaviorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <param name="serviceType">Closed behavior interface type</param>
    /// <param name="implementationType">Closed behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        BehaviorsToRegister.Add(new(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Registers an open behavior type against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic behavior type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IPipelineBehavior<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            BehaviorsToRegister.Add(new(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers multiple open behavior types against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorTypes">An open generic behavior type list includes multiple open generic behavior types.</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddOpenBehaviors(IEnumerable<Type> openBehaviorTypes, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        foreach (var openBehaviorType in openBehaviorTypes)
        {
            AddOpenBehavior(openBehaviorType, serviceLifetime);
        }

        return this;
    }

    /// <summary>
    /// Registers open behaviors against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviors">An open generic behavior list includes multiple <see cref="OpenBehavior"/> open generic behaviors.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddOpenBehaviors(IEnumerable<OpenBehavior> openBehaviors)
    {
        foreach (var openBehavior in openBehaviors)
        {
            AddOpenBehavior(openBehavior.OpenBehaviorType, openBehavior.ServiceLifetime);
        }

        return this;
    }

    /// <summary>
    /// Register a closed stream behavior type
    /// </summary>
    /// <typeparam name="TServiceType">Closed stream behavior interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed stream behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddStreamBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddStreamBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed stream behavior type
    /// </summary>
    /// <param name="serviceType">Closed stream behavior interface type</param>
    /// <param name="implementationType">Closed stream behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddStreamBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        StreamBehaviorsToRegister.Add(new(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed stream behavior type against all <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed stream behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddStreamBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddStreamBehavior(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed stream behavior type against all <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed stream behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddStreamBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IStreamPipelineBehavior<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IStreamPipelineBehavior<,>).FullName}");
        }

        foreach (var implementedBehaviorType in implementedGenericInterfaces)
        {
            StreamBehaviorsToRegister.Add(new(implementedBehaviorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers an open stream behavior type against the <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic stream behavior type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddOpenStreamBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IStreamPipelineBehavior<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IStreamPipelineBehavior<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            StreamBehaviorsToRegister.Add(new(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed request pre-processor type
    /// </summary>
    /// <typeparam name="TServiceType">Closed request pre-processor interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed request pre processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPreProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request pre-processor type
    /// </summary>
    /// <param name="serviceType">Closed request pre-processor interface type</param>
    /// <param name="implementationType">Closed request pre processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPreProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        RequestPreProcessorsToRegister.Add(new(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed request pre-processor type against all <see cref="IRequestPreProcessor{TRequest}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed request pre processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPreProcessor<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request pre-processor type against all <see cref="IRequestPreProcessor{TRequest}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed request pre processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPreProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPreProcessor<>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
        }

        foreach (var implementedPreProcessorType in implementedGenericInterfaces)
        {
            RequestPreProcessorsToRegister.Add(new(implementedPreProcessorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers an open request pre-processor type against the <see cref="IRequestPreProcessor{TRequest}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic request pre processor type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddOpenRequestPreProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPreProcessor<>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            RequestPreProcessorsToRegister.Add(new(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed request post processor type
    /// </summary>
    /// <typeparam name="TServiceType">Closed request post processor interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed request post processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPostProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request post processor type
    /// </summary>
    /// <param name="serviceType">Closed request post processor interface type</param>
    /// <param name="implementationType">Closed request post processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPostProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        RequestPostProcessorsToRegister.Add(new(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed request post processor type against all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed request post processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPostProcessor<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request post processor type against all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed request post processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddRequestPostProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPostProcessor<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
        }

        foreach (var implementedPostProcessorType in implementedGenericInterfaces)
        {
            RequestPostProcessorsToRegister.Add(new(implementedPostProcessorType, implementationType, serviceLifetime));
        }
        return this;
    }

    /// <summary>
    /// Registers an open request post processor type against the <see cref="IRequestPostProcessor{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic request post processor type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public FlumeConfiguration AddOpenRequestPostProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPostProcessor<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            RequestPostProcessorsToRegister.Add(new(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }


}
