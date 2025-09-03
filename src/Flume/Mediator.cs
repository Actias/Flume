using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.Internal;
using Flume.NotificationPublishers;

namespace Flume;

/// <summary>
/// Default mediator implementation with optimized performance and simplified logic
/// </summary>
/// <remarks>
/// Initializes a new instance of the Mediator class
/// </remarks>
/// <param name="serviceProvider">The service provider for resolving dependencies</param>
/// <param name="publisher">The notification publisher</param>
public class Mediator(IServiceProvider serviceProvider, INotificationPublisher publisher) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly INotificationPublisher _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    
    // Thread-safe caches for handler wrappers to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, HandlerWrapper> RequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();
    private static readonly ConcurrentDictionary<Type, StreamRequestHandlerWrapper> StreamRequestHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    public Mediator(IServiceProvider serviceProvider) 
        : this(serviceProvider, new ForeachAwaitPublisher()) { }

    /// <summary>
    /// Sends a request and returns the response
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected</typeparam>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the request handler</returns>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(request.GetType(), CreateRequestHandlerWrapper<TResponse>);
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Sends a request without a response
    /// </summary>
    /// <typeparam name="TRequest">The type of request to send</typeparam>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the completion of the request</returns>
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerWrapper)RequestHandlers.GetOrAdd(request.GetType(), CreateRequestHandlerWrapper);
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Sends a request using object-based dispatch
    /// </summary>
    /// <param name="request">The request object to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the request handler</returns>
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = RequestHandlers.GetOrAdd(request.GetType(), CreateRequestHandlerWrapperFromObject);
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification to all registered handlers
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish</typeparam>
    /// <param name="notification">The notification to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the completion of publishing</returns>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        return PublishNotification(notification, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification using object-based dispatch
    /// </summary>
    /// <param name="notification">The notification object to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the completion of publishing</returns>
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        
        return notification is INotification notification1
            ? PublishNotification(notification1, cancellationToken)
            : throw new ArgumentException($"Object must implement {nameof(INotification)}", nameof(notification));
    }

    /// <summary>
    /// Creates a stream for a stream request
    /// </summary>
    /// <typeparam name="TResponse">The type of response in the stream</typeparam>
    /// <param name="request">The stream request to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of responses</returns>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (StreamRequestHandlerWrapper<TResponse>)StreamRequestHandlers
            .GetOrAdd(request.GetType(), CreateStreamRequestHandlerWrapper<TResponse>);
        
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Creates a stream using object-based dispatch
    /// </summary>
    /// <param name="request">The stream request object to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of responses</returns>
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = StreamRequestHandlers.GetOrAdd(request.GetType(), CreateStreamRequestHandlerWrapperFromObject);

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Override in a derived class to control how the tasks are awaited. By default, the implementation calls the <see cref="INotificationPublisher"/>.
    /// </summary>
    /// <param name="handlerExecutors">Enumerable of tasks representing invoking each notification handler</param>
    /// <param name="notification">The notification being published</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing invoking all handlers</returns>
    protected virtual Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken) 
        => _publisher.Publish(handlerExecutors, notification, cancellationToken);

    private Task PublishNotification(INotification notification, CancellationToken cancellationToken = default)
    {
        var handler = NotificationHandlers.GetOrAdd(notification.GetType(), CreateNotificationHandlerWrapper);

        return handler.Handle(notification, _serviceProvider, cancellationToken);
    }

    // Factory methods for creating handler wrappers - optimized to reduce reflection overhead
    private static RequestHandlerWrapper<TResponse> CreateRequestHandlerWrapper<TResponse>(Type requestType)
    {
        var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));

        return (RequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;
    }

    private static RequestHandlerWrapper CreateRequestHandlerWrapper(Type requestType)
    {
        var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);

        return (RequestHandlerWrapper)Activator.CreateInstance(wrapperType)!;
    }

    private static HandlerWrapper CreateRequestHandlerWrapperFromObject(Type requestType)
    {
        // Check if it implements IRequest<TResponse>
        var requestInterfaceType = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        Type wrapperType;

        if (requestInterfaceType != null)
        {
            var responseType = requestInterfaceType.GetGenericArguments()[0];

            wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);

            return (HandlerWrapper)Activator.CreateInstance(wrapperType)!;
        }

        // Check if it implements IRequest (void response)
        if (!typeof(IRequest).IsAssignableFrom(requestType))
        {
            throw new ArgumentException(
                $"Type {requestType.Name} does not implement IRequest or IRequest<TResponse>",
                nameof(requestType)
            );
        }

        wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);

        return (HandlerWrapper)Activator.CreateInstance(wrapperType)!;
    }

    private static NotificationHandlerWrapper CreateNotificationHandlerWrapper(Type notificationType)
    {
        var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType);

        return (NotificationHandlerWrapper)Activator.CreateInstance(wrapperType)!;
    }

    private static StreamRequestHandlerWrapper<TResponse> CreateStreamRequestHandlerWrapper<TResponse>(Type requestType)
    {
        var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));

        return (StreamRequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;
    }

    private static StreamRequestHandlerWrapper CreateStreamRequestHandlerWrapperFromObject(Type requestType)
    {
        var requestInterfaceType = requestType.GetInterfaces()
                                       .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>))
            ?? throw new ArgumentException($"Type {requestType.Name} does not implement IStreamRequest<TResponse>", nameof(requestType));
        

        var responseType = requestInterfaceType.GetGenericArguments()[0];
        var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);

        return (StreamRequestHandlerWrapper)Activator.CreateInstance(wrapperType)!;
    }
}
