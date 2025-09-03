using System;
using System.Linq;
using Flume.Handlers;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Flume;

/// <summary>
/// Extensions for Microsoft.Extensions.DependencyInjection
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Add Flume to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">Optional configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, Action<FlumeConfiguration>? config = null)
    {
        var configuration = new FlumeConfiguration();
        config?.Invoke(configuration);

        // Register the mediator
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(provider => provider.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(provider => provider.GetRequiredService<IMediator>());

        // Register handlers
        if (configuration.ScanForHandlers)
        {
            services.AddHandlers();
        }

        return services;
    }

    /// <summary>
    /// Add Flume to the service collection with assembly scanning
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, params System.Reflection.Assembly[] assemblies)
    {
        services.AddFlume();
        
        if (assemblies.Length > 0)
        {
            services.AddHandlers(assemblies);
        }

        return services;
    }

    private static void AddHandlers(this IServiceCollection services, System.Reflection.Assembly[]? assemblies = null)
    {
        var assembliesToScan = assemblies ?? [System.Reflection.Assembly.GetCallingAssembly()];

        foreach (var assembly in assembliesToScan)
        {
            // Register request handlers
            var requestHandlerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                            t.GetInterfaces().Any(i => i.IsGenericType
                                                       && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                                                           || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
                );

            foreach (var handlerType in requestHandlerTypes)
            {
                var interfaces = handlerType
                    .GetInterfaces()
                    .Where(i => i.IsGenericType
                                && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                                    || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))
                    );

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }

            // Register notification handlers
            var notificationHandlerTypes = assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                );

            foreach (var handlerType in notificationHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }

            // Register stream request handlers
            var streamRequestHandlerTypes = assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>))
                );

            foreach (var handlerType in streamRequestHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }

            // Register pipeline behaviors
            var pipelineBehaviorTypes = assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && t.GetInterfaces()
                                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                );

            foreach (var behaviorType in pipelineBehaviorTypes)
            {
                var interfaces = behaviorType
                    .GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, behaviorType);
                }
            }
        }
    }
}

/// <summary>
/// Configuration for Flume
/// </summary>
public class FlumeConfiguration
{
    /// <summary>
    /// Whether to automatically scan for handlers in the calling assembly
    /// </summary>
    public bool ScanForHandlers { get; set; } = true;
}
