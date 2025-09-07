using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1303

namespace Flume.Samples;

/// <summary>
/// Performance benchmarks for Flume optimizations
/// </summary>
public class PerformanceBenchmarks
{
    private readonly IMediator _mediator;

    public PerformanceBenchmarks()
    {
        var services = new ServiceCollection();
        
        // Configure Flume with optimizations enabled
        services.AddFlume(cfg => 
        {
            cfg.EnableObjectPooling = true;
            cfg.EnablePipelineCompilation = true;
            cfg.EnableTypeCaching = true;
            cfg.MaxCacheSize = 1000;
        });
        
        // Register sample handlers
        services.AddScoped<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddScoped<IRequestHandler<TestRequestVoid>, TestRequestVoidHandler>();
        services.AddScoped<INotificationHandler<TestNotification>, TestNotificationHandler>();
        
        // Register pipeline behaviors
        services.AddScoped<IPipelineBehavior<TestRequest, string>, TestPipelineBehavior<TestRequest, string>>();
        services.AddScoped<IPipelineBehavior<TestRequestVoid, Unit>, TestPipelineBehaviorVoid<TestRequestVoid>>();
        
        var serviceProvider = services.BuildServiceProvider();

        _mediator = serviceProvider.GetRequiredService<IMediator>();
    }
    
    /// <summary>
    /// Benchmark request handling with response
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkRequestWithResponse(int iterations = 10000)
    {
        var request = new TestRequest { Message = "Hello World" };
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var response = await _mediator.Send(request);
            if (response != "Hello World processed")
            {
                throw new InvalidOperationException("Unexpected response");
            }
        }
        
        stopwatch.Stop();
        
        return new()
        {
            Operation = "Request with Response",
            Iterations = iterations,
            TotalTime = stopwatch.Elapsed,
            AverageTimePerOperation = stopwatch.Elapsed.TotalMilliseconds / iterations,
            OperationsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Benchmark request handling without response
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkRequestWithoutResponse(int iterations = 10000)
    {
        var request = new TestRequestVoid { Message = "Hello World" };
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            await _mediator.Send(request);
        }
        
        stopwatch.Stop();
        
        return new()
        {
            Operation = "Request without Response",
            Iterations = iterations,
            TotalTime = stopwatch.Elapsed,
            AverageTimePerOperation = stopwatch.Elapsed.TotalMilliseconds / iterations,
            OperationsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Benchmark notification publishing
    /// </summary>
    public async Task<BenchmarkResult> BenchmarkNotificationPublishing(int iterations = 10000)
    {
        var notification = new TestNotification { Message = "Hello World" };
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            await _mediator.Publish(notification);
        }
        
        stopwatch.Stop();
        
        return new()
        {
            Operation = "Notification Publishing",
            Iterations = iterations,
            TotalTime = stopwatch.Elapsed,
            AverageTimePerOperation = stopwatch.Elapsed.TotalMilliseconds / iterations,
            OperationsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds
        };
    }
    
    /// <summary>
    /// Run all benchmarks
    /// </summary>
    public async Task<List<BenchmarkResult>> RunAllBenchmarks()
    {
        var results = new List<BenchmarkResult>();
        
        Console.WriteLine("Running Flume Performance Benchmarks...");
        Console.WriteLine("=====================================");
        
        // Warm up
        await _mediator.Send(new TestRequest { Message = "Warmup" });
        await _mediator.Send(new TestRequestVoid { Message = "Warmup" });
        await _mediator.Publish(new TestNotification { Message = "Warmup" });
        
        // Run benchmarks
        results.Add(await BenchmarkRequestWithResponse());
        results.Add(await BenchmarkRequestWithoutResponse());
        results.Add(await BenchmarkNotificationPublishing());
        
        // Display results
        foreach (var result in results)
        {
            Console.WriteLine($"{result.Operation}:");
            Console.WriteLine($"  Iterations: {result.Iterations:N0}");
            Console.WriteLine($"  Total Time: {result.TotalTime.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Average Time: {result.AverageTimePerOperation:F4} ms");
            Console.WriteLine($"  Operations/Second: {result.OperationsPerSecond:F0}");
            Console.WriteLine();
        }
        
        return results;
    }
}

/// <summary>
/// Benchmark result data
/// </summary>
public class BenchmarkResult
{
    public string Operation { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public TimeSpan TotalTime { get; set; }
    public double AverageTimePerOperation { get; set; }
    public double OperationsPerSecond { get; set; }
}

// Sample request and handler implementations for benchmarking
public class TestRequest : IRequest<string>
{
    public string Message { get; set; } = string.Empty;
}

public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"{request.Message} processed");
    }
}

public class TestRequestVoid : IRequest
{
    public string Message { get; set; } = string.Empty;
}

public class TestRequestVoidHandler : IRequestHandler<TestRequestVoid>
{
    public Task Handle(TestRequestVoid request, CancellationToken cancellationToken = default)
    {
        // Simulate some work
        return Task.CompletedTask;
    }
}

public class TestNotification : INotification
{
    public string Message { get; set; } = string.Empty;
}

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken = default)
    {
        // Simulate some work
        return Task.CompletedTask;
    }
}

public class TestPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Simulate pipeline behavior work
        return next(cancellationToken);
    }
}

public class TestPipelineBehaviorVoid<TRequest> : IPipelineBehavior<TRequest, Unit>
    where TRequest : IRequest
{
    public Task<Unit> Handle(TRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
    {
        // Simulate pipeline behavior work
        return next(cancellationToken);
    }
}
