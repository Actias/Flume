using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Registration;

/// <summary>
/// Extension methods for registering Flume services with performance optimizations
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Flume services to the service collection with performance optimizations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, Action<FlumeConfiguration>? configure = null)
    {
        var config = new FlumeConfiguration();
        configure?.Invoke(config);
        
        // Register core services
        services.AddSingleton<IMediator, Mediator>();
        services.AddSingleton<ISender, Mediator>();
        services.AddSingleton<IPublisher, Mediator>();
        
        // Register notification publisher
        services.AddSingleton<INotificationPublisher, NotificationPublishers.ForeachAwaitPublisher>();
        
        // Parallel type discovery and registration for better startup performance
        if (config.AssembliesToRegister.Count > 0)
        {
            foreach (var assembly in config.AssembliesToRegister)
            {
                var types = assembly.GetTypes();
                
                // Parallel registration of handlers
                var handlerTypes = types.AsParallel()
                    .Where(t => IsHandlerType(t))
                    .ToArray();
                    
                Parallel.ForEach(handlerTypes, handlerType => RegisterHandler(services, handlerType));
                
                // Parallel registration of pipeline behaviors
                var behaviorTypes = types.AsParallel()
                    .Where(t => IsPipelineBehaviorType(t))
                    .ToArray();
                    
                Parallel.ForEach(behaviorTypes, behaviorType => RegisterPipelineBehavior(services, behaviorType));
            }
        }
        
        return services;
    }
    
    /// <summary>
    /// Add Flume services to the service collection with assembly scanning
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">Assembly to scan for handlers and behaviors</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, Assembly assembly, Action<FlumeConfiguration>? configure = null)
    {
        var config = new FlumeConfiguration();
        config.RegisterServicesFromAssembly(assembly);
        configure?.Invoke(config);
        return services.AddFlume(configure);
    }
    
    private static bool IsHandlerType(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            return false;
            
        return type.GetInterfaces().Any(i => 
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>)));
    }
    
    private static bool IsPipelineBehaviorType(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            return false;
            
        return type.GetInterfaces().Any(i => 
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>)));
    }
    
    private static void RegisterHandler(IServiceCollection services, Type handlerType)
    {
        var interfaces = handlerType.GetInterfaces();
        
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType)
            {
                var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
                
                if (genericTypeDefinition == typeof(IRequestHandler<,>) ||
                    genericTypeDefinition == typeof(IRequestHandler<>) ||
                    genericTypeDefinition == typeof(INotificationHandler<>) ||
                    genericTypeDefinition == typeof(IStreamRequestHandler<,>))
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }
        }
    }
    
    private static void RegisterPipelineBehavior(IServiceCollection services, Type behaviorType)
    {
        var interfaces = behaviorType.GetInterfaces();
        
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType)
            {
                var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
                
                if (genericTypeDefinition == typeof(IPipelineBehavior<,>) ||
                    genericTypeDefinition == typeof(IStreamPipelineBehavior<,>))
                {
                    services.AddScoped(interfaceType, behaviorType);
                }
            }
        }
    }
}
