using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flume.NotificationPublishers;
using Flume.Wrappers;

namespace Flume;

/// <summary>
/// Default mediator implementation.
/// Wrapper instances are cached per closed request type. Handlers and behaviors are resolved on each call.
/// </summary>
public class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, HandlerWrapper> RequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();
    private static readonly ConcurrentDictionary<Type, StreamRequestHandlerWrapper> StreamRequestHandlers = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationPublisher _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Scoped handlers are resolved from this provider on each call.</param>
    /// <param name="publisher">Notification publisher. Defaults to <see cref="ForeachAwaitPublisher"/> when the other constructor is used.</param>
    public Mediator(IServiceProvider serviceProvider, INotificationPublisher publisher)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    public Mediator(IServiceProvider serviceProvider)
        : this(serviceProvider, new ForeachAwaitPublisher())
    {
    }

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

        var handler = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType => CreateRequestHandlerWrapper(requestType, typeof(TResponse)));

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Sends a request without a response
    /// </summary>
    /// <typeparam name="TRequest">The type of request to send</typeparam>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the completion of the request</returns>
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerWrapper<Unit>)RequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType => CreateRequestHandlerWrapper(requestType, typeof(Unit)));

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
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
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

        var handler = (StreamRequestHandlerWrapper<TResponse>)StreamRequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType => CreateStreamRequestHandlerWrapper(requestType, typeof(TResponse)));

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
    protected virtual Task PublishCore(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
        => _publisher.Publish(handlerExecutors, notification, cancellationToken);

    private Task PublishNotification(INotification notification, CancellationToken cancellationToken)
    {
        var handler = NotificationHandlers.GetOrAdd(notification.GetType(), static notificationType =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType);
            return (NotificationHandlerWrapper)(Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper for type {notificationType}."));
        });

        return handler.Handle(notification, _serviceProvider, PublishCore, cancellationToken);
    }

    private static HandlerWrapper CreateRequestHandlerWrapper(Type requestType, Type responseType)
    {
        var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
        return (HandlerWrapper)(Activator.CreateInstance(wrapperType)
            ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}."));
    }

    private static HandlerWrapper CreateRequestHandlerWrapperFromObject(Type requestType)
    {
        var responseType = requestType
            .GetInterfaces()
            .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()[0];

        if (responseType is null)
        {
            throw new ArgumentException(
                $"Type {requestType.Name} does not implement IRequest or IRequest<TResponse>",
                nameof(requestType));
        }

        return CreateRequestHandlerWrapper(requestType, responseType);
    }

    private static StreamRequestHandlerWrapper CreateStreamRequestHandlerWrapper(Type requestType, Type responseType)
    {
        var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
        return (StreamRequestHandlerWrapper)(Activator.CreateInstance(wrapperType)
            ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}."));
    }

    private static StreamRequestHandlerWrapper CreateStreamRequestHandlerWrapperFromObject(Type requestType)
    {
        var responseType = requestType
            .GetInterfaces()
            .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>))
            ?.GetGenericArguments()[0];

        if (responseType is null)
        {
            throw new ArgumentException(
                $"Type {requestType.Name} does not implement IStreamRequest<TResponse>",
                nameof(requestType));
        }

        return CreateStreamRequestHandlerWrapper(requestType, responseType);
    }
}