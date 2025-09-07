using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Flume.Internal;

/// <summary>
/// Pre-compiles pipeline behaviors to improve runtime performance
/// </summary>
public static class PipelineCompiler
{
    private static readonly ConcurrentDictionary<Type, Delegate> CompiledPipelines = new();
    
    /// <summary>
    /// Compile a pipeline for a specific request/response type combination
    /// </summary>
    public static RequestHandlerDelegate<TResponse> CompilePipeline<TRequest, TResponse>(
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
        RequestHandlerDelegate<TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        var behaviorArray = behaviors.ToArray();
        
        if (behaviorArray.Length == 0)
        {
            return handler;
        }
        
        // Create a compiled delegate that chains all behaviors
        var result = CreateCompiledDelegate(behaviorArray, handler);
        return result;
    }
    
    /// <summary>
    /// Get or create a compiled pipeline delegate
    /// </summary>
    public static RequestHandlerDelegate<TResponse> GetOrCreateCompiledPipeline<TRequest, TResponse>(
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
        RequestHandlerDelegate<TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        var key = typeof(TRequest);
        return (RequestHandlerDelegate<TResponse>)CompiledPipelines.GetOrAdd(key, _ => 
            CompilePipeline(behaviors, handler));
    }
    
    private static RequestHandlerDelegate<TResponse> CreateCompiledDelegate<TRequest, TResponse>(
        IPipelineBehavior<TRequest, TResponse>[] behaviors,
        RequestHandlerDelegate<TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        // Start with the handler
        var current = handler;
        
        // Chain behaviors in reverse order (last behavior wraps the handler)
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = current;
            
            // Create a compiled delegate for this behavior
            current = (ct) => behavior.Handle(default!, next, ct);
        }
        
        return current;
    }
    
    /// <summary>
    /// Clear all compiled pipeline delegates
    /// </summary>
    public static void Clear()
    {
        CompiledPipelines.Clear();
    }
}
