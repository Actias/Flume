using BenchmarkDotNet.Attributes;
using Flume.Comparison.Flume;
using Flume.Comparison.MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Comparison.Benchmarks;

/// <summary>
/// Benchmark class for performance comparison between Flume and Flume
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MediatorBenchmarks
{
    private global::MediatR.IMediator _mediatR = null!;
    private IMediator _flume = null!;
    private MediatRRequest _mediatRRequest = null!;
    private FlumeRequest _flumeRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup MediatR
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        var mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatR = mediatRProvider.GetRequiredService<global::MediatR.IMediator>();

        // Setup Flume
        var flumeServices = new ServiceCollection();
        flumeServices.AddFlume(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        flumeServices.AddScoped<IRequestHandler<FlumeRequest, string>, FlumeHandler>();
        var flumeProvider = flumeServices.BuildServiceProvider();
        _flume = flumeProvider.GetRequiredService<IMediator>();

        _mediatRRequest = new("test message");
        _flumeRequest = new("test message");
    }

    [Benchmark]
    public async Task<string> MediatRSend()
    {
        return await _mediatR.Send(_mediatRRequest);
    }

    [Benchmark]
    public async Task<string> FlumeSend()
    {
        return await _flume.Send(_flumeRequest);
    }

    [Benchmark]
    public async Task<string> MediatRSendWithPipeline()
    {
        // Test with pipeline behaviors (if any are registered)
        return await _mediatR.Send(_mediatRRequest);
    }

    [Benchmark]
    public async Task<string> FlumeSendWithPipeline()
    {
        // Test with pipeline behaviors (if any are registered)
        return await _flume.Send(_flumeRequest);
    }

    [Benchmark]
    public async Task<string> MediatRSendConcurrent()
    {
        // Test concurrent access
        var tasks = new Task<string>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _mediatR.Send(_mediatRRequest);
        }
        var results = await Task.WhenAll(tasks);
        return results[0]; // Return first result for consistency
    }

    [Benchmark]
    public async Task<string> FlumeSendConcurrent()
    {
        // Test concurrent access
        var tasks = new Task<string>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _flume.Send(_flumeRequest);
        }
        var results = await Task.WhenAll(tasks);
        return results[0]; // Return first result for consistency
    }

    [Benchmark]
    public async Task<string> MediatRSendMemoryPressure()
    {
        // Test under memory pressure by creating temporary objects
        var tempObjects = new object[1000];
        for (int i = 0; i < 1000; i++)
        {
            tempObjects[i] = new { Id = i, Data = new string('x', 100) };
        }
        
        var result = await _mediatR.Send(_mediatRRequest);
        
        // Simulate memory pressure without explicit GC.Collect
        _ = tempObjects.Length; // Use the variable to avoid unused warning
        
        return result;
    }

    [Benchmark]
    public async Task<string> FlumeSendMemoryPressure()
    {
        // Test under memory pressure by creating temporary objects
        var tempObjects = new object[1000];
        for (int i = 0; i < 1000; i++)
        {
            tempObjects[i] = new { Id = i, Data = new string('x', 100) };
        }
        
        var result = await _flume.Send(_flumeRequest);
        
        // Simulate memory pressure without explicit GC.Collect
        _ = tempObjects.Length; // Use the variable to avoid unused warning
        
        return result;
    }
}
