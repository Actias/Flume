using System;
using System.Linq;
using Flume.Pipelines;
using Flume.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Flume;

/// <summary>
/// Extensions for adding Flume to an <see cref="IServiceCollection"/>
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers handlers and mediator types from the calling assembly
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services)
    {
        // Register the mediator
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(provider => provider.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(provider => provider.GetRequiredService<IMediator>());
        
        services.AddHandlers();

        return services;
    }

    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">The action used to configure the options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, Action<FlumeConfiguration> configuration)
    {
        var serviceConfig = new FlumeConfiguration();

        configuration.Invoke(serviceConfig);

        return services.AddFlume(serviceConfig);
    }

    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, FlumeConfiguration configuration)
    {
        if (configuration.AssembliesToRegister.Count == 0)
        {
            throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");
        }

        ServiceRegistrar.SetGenericRequestHandlerRegistrationLimitations(configuration);

        ServiceRegistrar.AddFlumeClassesWithTimeout(services, configuration);

        ServiceRegistrar.AddRequiredServices(services, configuration);

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