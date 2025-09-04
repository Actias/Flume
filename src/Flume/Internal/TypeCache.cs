using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Flume.Internal;

/// <summary>
/// Caches type information and pre-compiled delegates to reduce reflection overhead
/// </summary>
public static class TypeCache
{
    private static readonly ConcurrentDictionary<Type, HandlerInfo> HandlerCache = new();
    private static readonly ConcurrentDictionary<Type, ServiceInfo> ServiceCache = new();
    
    /// <summary>
    /// Get or create handler information for a request type
    /// </summary>
    public static HandlerInfo GetOrAddHandlerInfo(Type requestType)
    {
        return HandlerCache.GetOrAdd(requestType, CreateHandlerInfo);
    }
    
    /// <summary>
    /// Get or create service information for a service type
    /// </summary>
    public static ServiceInfo GetOrAddServiceInfo(Type serviceType)
    {
        return ServiceCache.GetOrAdd(serviceType, CreateServiceInfo);
    }
    
    private static HandlerInfo CreateHandlerInfo(Type requestType)
    {
        // Check if it implements IRequest<TResponse>
        var requestInterfaceType = requestType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterfaceType is null)
        {
            return typeof(IRequest).IsAssignableFrom(requestType)
                ? new(requestType, typeof(Unit), false)
                : throw new ArgumentException($"Type {requestType.Name} does not implement IRequest or IRequest<TResponse>");
        }

        var responseType = requestInterfaceType.GetGenericArguments()[0];

        return new(requestType, responseType, true);

        // Check if it implements IRequest (void response)
    }
    
    private static ServiceInfo CreateServiceInfo(Type serviceType)
    {
        var getServiceMethod = typeof(IServiceProvider).GetMethod("GetService", [typeof(Type)])!;
        var getServicesMethod = typeof(IServiceProvider).GetMethod("GetServices", [typeof(Type)])!;
        var getRequiredServiceMethod = typeof(IServiceProvider).GetMethod("GetRequiredService", [typeof(Type)])!;
        
        // Pre-compile delegates for service resolution
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        
        var getServiceDelegate = Expression.Lambda<Func<IServiceProvider, object?>>(
            Expression.Call(serviceProviderParam, getServiceMethod, Expression.Constant(serviceType)),
            serviceProviderParam).Compile();
            
        var getServicesDelegate = Expression.Lambda<Func<IServiceProvider, IEnumerable<object?>>>(
            Expression.Call(serviceProviderParam, getServicesMethod, Expression.Constant(serviceType)),
            serviceProviderParam).Compile();
            
        var getRequiredServiceDelegate = Expression.Lambda<Func<IServiceProvider, object?>>(
            Expression.Call(serviceProviderParam, getRequiredServiceMethod, Expression.Constant(serviceType)),
            serviceProviderParam).Compile();
        
        return new(serviceType, getServiceDelegate, getServicesDelegate, getRequiredServiceDelegate);
    }
    
    /// <summary>
    /// Clear all cached type information
    /// </summary>
    public static void Clear()
    {
        HandlerCache.Clear();
        ServiceCache.Clear();
    }
}

    /// <summary>
    /// Cached information about a request handler
    /// </summary>
    public readonly struct HandlerInfo(Type requestType, Type responseType, bool hasResponse) 
        : IEquatable<HandlerInfo>
    {
    /// <summary>
    /// Gets the type of the request associated with this instance.
    /// </summary>
    public Type RequestType { get; } = requestType;

    /// <summary>
    /// Gets the type of the response associated with this instance.
    /// </summary>
    public Type ResponseType { get; } = responseType;

    /// <summary>
    /// Indicates whether a response has been received.
    /// </summary>
    public bool HasResponse { get; } = hasResponse;

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current instance; otherwise, <see
    /// langword="false"/>.</returns>
    public override bool Equals(object? obj)
        => obj is HandlerInfo other && Equals(other);

    /// <summary>
    /// Determines whether the current instance is equal to another instance of <see cref="HandlerInfo"/>.
    /// </summary>
    /// <param name="other">The <see cref="HandlerInfo"/> instance to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified <see cref="HandlerInfo"/> instance is equal to the current instance;
    /// otherwise, <see langword="false"/>.</returns>
    public bool Equals(HandlerInfo other)
        => RequestType == other.RequestType
           && ResponseType == other.ResponseType
           && HasResponse == other.HasResponse;

    /// <summary>
    /// Returns a hash code for the current object based on its request type, response type, and response presence.
    /// </summary>
    /// <returns>An integer representing the hash code of the current object.</returns>
    public override int GetHashCode()
        => HashCode.Combine(RequestType, ResponseType, HasResponse);

    /// <summary>
    /// Determines whether two <see cref="HandlerInfo"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="HandlerInfo"/> instance to compare.</param>
    /// <param name="right">The second <see cref="HandlerInfo"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the specified <see cref="HandlerInfo"/> instances are equal; otherwise, <see
    /// langword="false"/>.</returns>
    public static bool operator ==(HandlerInfo left, HandlerInfo right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="HandlerInfo"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="HandlerInfo"/> instance to compare.</param>
    /// <param name="right">The second <see cref="HandlerInfo"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the two instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(HandlerInfo left, HandlerInfo right)
        => !left.Equals(right);
}

/// <summary>
/// Cached information about service resolution
/// </summary>
public readonly struct ServiceInfo(Type serviceType, Func<IServiceProvider, object?> getService,
    Func<IServiceProvider, IEnumerable<object?>> getServices,
    Func<IServiceProvider, object?> getRequiredService)
    : IEquatable<ServiceInfo>
{
    /// <summary>
    /// The service type being resolved
    /// </summary>
    public Type ServiceType { get; } = serviceType;

    /// <summary>
    /// A delegate that retrieves a service object of a specified type from the provided <see cref="IServiceProvider"/>.
    /// </summary>
    /// <remarks>The delegate takes an <see cref="IServiceProvider"/> as input and returns the requested
    /// service object,  or <see langword="null"/> if the service is not available.</remarks>
    public Func<IServiceProvider, object?> GetService { get; } = getService;

    /// <summary>
    /// A delegate that retrieves a collection of services from the specified <see cref="IServiceProvider"/>.
    /// </summary>
    /// <remarks>The delegate takes an <see cref="IServiceProvider"/> as input and returns an <see
    /// cref="IEnumerable{T}"/>  containing the resolved services. The collection may include null values if any service
    /// resolution results in null.</remarks>
    public Func<IServiceProvider, IEnumerable<object?>> GetServices { get; } = getServices;

    /// <summary>
    /// A delegate that retrieves a required service from the specified <see cref="IServiceProvider"/>.
    /// </summary>
    /// <remarks>This delegate is expected to return a non-null service instance when invoked with a valid
    /// <see cref="IServiceProvider"/>. If the required service is not available, an exception may be thrown depending
    /// on the implementation of the provided delegate.</remarks>
    public Func<IServiceProvider, object?> GetRequiredService { get; } = getRequiredService;

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current instance; otherwise, <see
    /// langword="false"/>.</returns>
    public override bool Equals(object? obj)
        => obj is HandlerInfo other && Equals(other);

    /// <summary>
    /// Determines whether the current instance is equal to another instance of <see cref="ServiceInfo"/>.
    /// </summary>
    /// <param name="other">The <see cref="ServiceInfo"/> instance to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the specified <see cref="ServiceInfo"/> instance is equal to the current instance;
    /// otherwise, <see langword="false"/>.</returns>
    public bool Equals(ServiceInfo other)
        => ServiceType == other.ServiceType
           && GetService == other.GetService
           && GetServices == other.GetServices
           && GetRequiredService == other.GetRequiredService;

    /// <summary>
    /// Returns a hash code for the current object based on its request type, response type, and response presence.
    /// </summary>
    /// <returns>An integer representing the hash code of the current object.</returns>
    public override int GetHashCode()
        => HashCode.Combine(ServiceType, GetService, GetServices, GetRequiredService);

    /// <summary>
    /// Determines whether two <see cref="ServiceInfo"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ServiceInfo"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ServiceInfo"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the specified <see cref="ServiceInfo"/> instances are equal; otherwise, <see
    /// langword="false"/>.</returns>
    public static bool operator ==(ServiceInfo left, ServiceInfo right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="ServiceInfo"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ServiceInfo"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ServiceInfo"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the two instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ServiceInfo left, ServiceInfo right)
        => !left.Equals(right);
}
