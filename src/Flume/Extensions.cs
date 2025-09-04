using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        var assembly = Assembly.GetCallingAssembly();

        services.AddFlume(cfg => cfg.RegisterServicesFromAssembly(assembly));

        return services;
    }

    /// <summary>
    /// Registers handlers and mediator types from the specified assembly
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Target assembly containing handlers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, Assembly assembly)
    {
        services.AddFlume(cfg => cfg.RegisterServicesFromAssembly(assembly));

        return services;
    }

    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Target assemblies containing handlers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        services.AddFlume(cfg => cfg.RegisterServicesFromAssemblies(assemblies.ToArray()));

        return services;
    }

    /// <summary>
    /// Registers handlers and mediator types by type
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="type">The type used to determine the assembly to add.</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddFlume(this IServiceCollection services, Type type)
    {
        services.AddFlume(cfg => cfg.RegisterServicesFromAssemblyContaining(type));

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
}