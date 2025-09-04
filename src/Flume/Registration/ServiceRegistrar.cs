using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;



using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flume.Registration;

/// <summary>
/// Provides methods for registering Flume and pipeline handler services with configurable limitations and timeouts.
/// </summary>
public static class ServiceRegistrar
{
    private static int _maxGenericTypeParameters;
    private static int _maxTypesClosing;
    private static int _maxGenericTypeRegistrations;
    private static int _registrationTimeout; 

    /// <summary>
    /// Sets the limitations for generic request handler registration based on the provided <see cref="FlumeConfiguration"/>.
    /// </summary>
    /// <param name="configuration">The configuration containing the registration limitations.</param>
    public static void SetGenericRequestHandlerRegistrationLimitations(FlumeConfiguration configuration)
    {
        _maxGenericTypeParameters = configuration.MaxGenericTypeParameters;
        _maxTypesClosing = configuration.MaxTypesClosing;
        _maxGenericTypeRegistrations = configuration.MaxGenericTypeRegistrations;
        _registrationTimeout = configuration.RegistrationTimeout;
    }

    /// <summary>
    /// Registers Flume classes and pipeline handler services with a configurable timeout.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">The Flume configuration containing registration options and limitations.</param>
    /// <exception cref="TimeoutException">Thrown if the registration process exceeds the configured timeout.</exception>
    public static void AddFlumeClassesWithTimeout(IServiceCollection services, FlumeConfiguration configuration)
    {

        using var cts = new CancellationTokenSource(_registrationTimeout);

        try
        {
            AddFlumeClasses(services, configuration, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("The generic handler registration process timed out.");
        }
    }

    /// <summary>
    /// Registers Flume classes and pipeline handler services.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">The Flume configuration containing registration options and limitations.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the registration to complete.</param>
    public static void AddFlumeClasses(IServiceCollection services, FlumeConfiguration configuration, CancellationToken cancellationToken = default)
    {   
        var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>), services, assembliesToScan, true, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IStreamRequestHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>), services, assembliesToScan, true, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionAction<,>), services, assembliesToScan, true, configuration, cancellationToken);

        if (configuration.AutoRegisterRequestProcessors)
        {
            ConnectImplementationsToTypesClosing(typeof(IRequestPreProcessor<>), services, assembliesToScan, true, configuration, cancellationToken);
            ConnectImplementationsToTypesClosing(typeof(IRequestPostProcessor<,>), services, assembliesToScan, true, configuration, cancellationToken);
        }

        var multiOpenInterfaces = new List<Type>
        {
            typeof(INotificationHandler<>),
            typeof(IRequestExceptionHandler<,,>),
            typeof(IRequestExceptionAction<,>)
        };

        if (configuration.AutoRegisterRequestProcessors)
        {
            multiOpenInterfaces.Add(typeof(IRequestPreProcessor<>));
            multiOpenInterfaces.Add(typeof(IRequestPostProcessor<,>));
        }

        foreach (var multiOpenInterface in multiOpenInterfaces)
        {
            var arity = multiOpenInterface.GetGenericArguments().Length;

            var concretions = assembliesToScan
                .SelectMany(a => a.DefinedTypes)
                .Where(type => type.FindInterfacesThatClose(multiOpenInterface).Any())
                .Where(type => type.IsConcrete() && type.IsOpenGeneric())
                .Where(type => type.GetGenericArguments().Length == arity)
                .Where(configuration.TypeEvaluator)
                .ToList();

            foreach (var type in concretions)
            {
                services.AddTransient(multiOpenInterface, type);
            }
        }
    }

    private static void ConnectImplementationsToTypesClosing(
        Type openRequestInterface,
        IServiceCollection services,
        IReadOnlyList<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        FlumeConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var concretions = new List<Type>();
        var interfaces = new List<Type>();
        var genericConcretions = new List<Type>();
        var genericInterfaces = new List<Type>();

        var types = assembliesToScan
            .SelectMany(a => a.DefinedTypes)
            .Where(t => !t.ContainsGenericParameters || configuration.RegisterGenericHandlers)
            .Where(t => t.IsConcrete() && t.FindInterfacesThatClose(openRequestInterface).Any())
            .Where(configuration.TypeEvaluator)
            .ToList();        

        foreach (var type in types)
        {
            var interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();

            if (!type.IsOpenGeneric())
            {
                concretions.Add(type);

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaces.Fill(interfaceType);
                }
            }
            else
            {
                genericConcretions.Add(type);
                foreach (var interfaceType in interfaceTypes)
                {
                    genericInterfaces.Fill(interfaceType);
                }
            }
        }

        foreach (var @interface in interfaces)
        {
            var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            if (addIfAlreadyExists)
            {
                foreach (var type in exactMatches)
                {
                    services.AddTransient(@interface, type);
                }
            }
            else
            {
                if (exactMatches.Count > 1)
                {
                    exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                }

                foreach (var type in exactMatches)
                {
                    services.TryAddTransient(@interface, type);
                }
            }

            if (!@interface.IsOpenGeneric())
            {
                AddConcretionsThatCouldBeClosed(@interface, concretions, services);
            }
        }

        foreach (var @interface in genericInterfaces)
        {
            var exactMatches = genericConcretions.Where(x => x.CanBeCastTo(@interface)).ToList();

            AddAllConcretionsThatClose(@interface, exactMatches, services, assembliesToScan, cancellationToken);
        }
    }

    private static bool IsMatchingWithInterface(Type? handlerType, Type? handlerInterface)
    {
        while (true)
        {
            if (handlerType is null || handlerInterface is null)
            {
                return false;
            }

            if (handlerType.IsInterface)
            {
                return handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments);
            }

            handlerType = handlerType.GetInterface(handlerInterface.Name);
        }
    }

    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (var type in concretions.Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // ignored - could not be constructed with the provided type arguments
            }
        }
    }

    private static (Type Service, Type Implementation) GetConcreteRegistrationTypes(Type openRequestHandlerInterface, Type concreteGenericTRequest, Type openRequestHandlerImplementation)
    {
        var closingTypes = concreteGenericTRequest.GetGenericArguments();

        var concreteTResponse = concreteGenericTRequest.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()
            .FirstOrDefault();

        var typeDefinition = openRequestHandlerInterface.GetGenericTypeDefinition();

        var serviceType = concreteTResponse != null ?
            typeDefinition.MakeGenericType(concreteGenericTRequest, concreteTResponse) :
            typeDefinition.MakeGenericType(concreteGenericTRequest);

        return (serviceType, openRequestHandlerImplementation.MakeGenericType(closingTypes));
    }

    private static List<Type>? GetConcreteRequestTypes(Type openRequestHandlerInterface, Type openRequestHandlerImplementation, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        //request generic type constraints       
        var constraintsForEachParameter = openRequestHandlerImplementation
            .GetGenericArguments()
            .Select(x => x.GetGenericParameterConstraints())
            .ToList();

        var typesThatCanCloseForEachParameter = constraintsForEachParameter
            .Select(constraints => assembliesToScan
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Where(t => constraints.All(constraint => constraint.IsAssignableFrom(t))).ToList()
            ).ToList();

        var requestType = openRequestHandlerInterface.GenericTypeArguments[0];

        if (requestType.IsGenericParameter)
        {
            return null;
        }

        var requestGenericTypeDefinition = requestType.GetGenericTypeDefinition();
              
        var combinations = GenerateCombinations(requestType, typesThatCanCloseForEachParameter, 0, cancellationToken);

        return [.. combinations.Select(types => requestGenericTypeDefinition.MakeGenericType([.. types]))];
    }

    /// <summary>
    /// Generates all possible combinations of types from the provided lists, subject to registration limitations.
    /// </summary>
    /// <param name="requestType">The generic request type being registered.</param>
    /// <param name="lists">A read-only collection of read-only collections of types, each representing possible types for a generic parameter.</param>
    /// <param name="depth">The current recursion depth (used internally).</param>
    /// <param name="cancellationToken">A cancellation token to observe while generating combinations.</param>
    /// <returns>A read-only list of read-only lists of types representing all valid combinations.</returns>
    public static IReadOnlyList<IReadOnlyList<Type>> GenerateCombinations(
        Type requestType,
        IReadOnlyList<IReadOnlyList<Type>> lists,
        int depth = 0,
        CancellationToken cancellationToken = default)
    {
        if (depth == 0)
        {
            // Initial checks
            if (_maxGenericTypeParameters > 0 && lists.Count > _maxGenericTypeParameters)
            {
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The number of generic type parameters exceeds the maximum allowed ({_maxGenericTypeParameters}).");
            }
            if (lists.Any(list => _maxTypesClosing > 0 && list.Count > _maxTypesClosing))
            {
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. One of the generic type parameter's count of types that can close exceeds the maximum length allowed ({_maxTypesClosing}).");
            }

            // Calculate the total number of combinations
            long totalCombinations = 1;

            foreach (var list in lists)
            {
                totalCombinations *= list.Count;

                if (_maxGenericTypeParameters > 0 && totalCombinations > _maxGenericTypeRegistrations)
                {
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The total number of generic type registrations exceeds the maximum allowed ({_maxGenericTypeRegistrations}).");
                }
            }
        }

        if (depth >= lists.Count)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        var currentList = lists[depth];
        var childCombinations = GenerateCombinations(requestType, lists, depth + 1, cancellationToken);
        var combinations = new List<IReadOnlyList<Type>>();

        foreach (var item in currentList)
        {
            foreach (var childCombination in childCombinations)
            {
                var currentCombination = new List<Type> { item };
                currentCombination.AddRange(childCombination);
                combinations.Add(currentCombination);
            }
        }

        return combinations;
    }

    private static void AddAllConcretionsThatClose(
        Type openRequestInterface,
        IReadOnlyList<Type> concretions,
        IServiceCollection services,
        IReadOnlyList<Assembly> assembliesToScan,
        CancellationToken cancellationToken
    )
    {
        foreach (var concretion in concretions)
        {   
            var concreteRequests = GetConcreteRequestTypes(openRequestInterface, concretion, assembliesToScan, cancellationToken);

            if (concreteRequests is null)
            {
                continue;
            }

            var registrationTypes = concreteRequests.Select(x => GetConcreteRegistrationTypes(openRequestInterface, x, concretion));

            foreach (var (service, implementation) in registrationTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                services.AddTransient(service, implementation);
            }
        }
    }

    internal static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        var openInterface = closedInterface.GetGenericTypeDefinition();
        var arguments = closedInterface.GenericTypeArguments;
        var concreteArguments = openConcretion.GenericTypeArguments;

        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    private static bool CanBeCastTo(this Type? pluggedType, Type pluginType)
    {
        return pluggedType is not null && (pluggedType == pluginType || pluginType.IsAssignableFrom(pluggedType));
    }

    private static bool IsOpenGeneric(this Type type)
    {
        return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
    }

    internal static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
    {
        return FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();
    }

    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type? pluggedType, Type templateType)
    {
        if (pluggedType == null) yield break;

        if (!pluggedType.IsConcrete()) yield break;

        if (templateType.IsInterface)
        {
            foreach (
                var interfaceType in
                pluggedType.GetInterfaces()
                    .Where(type => type.IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
            {
                yield return interfaceType;
            }
        }
        else if (pluggedType.BaseType!.IsGenericType &&
                 (pluggedType.BaseType!.GetGenericTypeDefinition() == templateType))
        {
            yield return pluggedType.BaseType!;
        }

        if (pluggedType.BaseType == typeof(object)) yield break;

        foreach (var interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType!, templateType))
        {
            yield return interfaceType;
        }
    }

    private static bool IsConcrete(this Type type)
    {
        return type is { IsAbstract: false, IsInterface: false };
    }

    private static void Fill<T>(this List<T> list, T value)
    {
        if (list.Contains(value))
        {
            return;
        }

        list.Add(value);
    }

    /// <summary>
    /// Registers the required Flume services and pipeline behaviors into the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="serviceConfiguration">The Flume configuration containing service registration options and behaviors.</param>
    public static void AddRequiredServices(IServiceCollection services, FlumeConfiguration serviceConfiguration)
    {
        // Use TryAdd, so any existing ServiceFactory/IMediator registration doesn't get overridden
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfiguration.MediatorImplementationType, serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));

        var notificationPublisherServiceDescriptor = serviceConfiguration.NotificationPublisherType != null
            ? new(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisherType, serviceConfiguration.Lifetime)
            : new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisher);

        services.TryAdd(notificationPublisherServiceDescriptor);

        // Register pre-processors, then post processors, then behaviors
        if (serviceConfiguration.RequestExceptionActionProcessorStrategy == RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions)
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
        }
        else
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
        }

        if (serviceConfiguration.RequestPreProcessorsToRegister.Count > 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(serviceConfiguration.RequestPreProcessorsToRegister);
        }

        if (serviceConfiguration.RequestPostProcessorsToRegister.Count > 0)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(serviceConfiguration.RequestPostProcessorsToRegister);
        }

        foreach (var serviceDescriptor in serviceConfiguration.BehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }

        foreach (var serviceDescriptor in serviceConfiguration.StreamBehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }
    }

    private static void RegisterBehaviorIfImplementationsExist(IServiceCollection services, Type behaviorType, Type subBehaviorType)
    {
        var hasAnyRegistrationsOfSubBehaviorType = services
            .Where(service => !service.IsKeyedService)
            .Select(service => service.ImplementationType)
            .OfType<Type>()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType)
            .Select(type => type.GetGenericTypeDefinition())
            .Any(type => type == subBehaviorType);

        if (hasAnyRegistrationsOfSubBehaviorType)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, ServiceLifetime.Transient));
        }
    }
}
