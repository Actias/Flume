using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flume.Wrappers;

/// <summary>
/// Base class for all handler wrappers
/// </summary>
internal abstract class HandlerWrapper
{
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}