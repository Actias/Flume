using System;
using System.Collections.Generic;
using System.Threading;

namespace Flume.Wrappers;

/// <summary>
/// Wrapper for stream request handlers
/// </summary>
internal abstract class StreamRequestHandlerWrapper
{
    public abstract IAsyncEnumerable<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Generic stream request handler wrapper for type-safe casting
/// </summary>
internal abstract class StreamRequestHandlerWrapper<TResponse> : StreamRequestHandlerWrapper
{
    public abstract IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

