using BenchmarkDotNet.Attributes;
using Flume.Comparison.Flume;
using Flume.Comparison.MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Comparison.Benchmarks;

/// <summary>
/// Memory allocation benchmarks for performance comparison between Flume and MediatR
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryAllocationBenchmarks
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
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MemoryAllocationBenchmarks).Assembly));
        var mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatR = mediatRProvider.GetRequiredService<global::MediatR.IMediator>();

        // Setup Flume
        var flumeServices = new ServiceCollection();
        flumeServices.AddFlume(cfg => cfg.RegisterServicesFromAssembly(typeof(MemoryAllocationBenchmarks).Assembly));
        flumeServices.AddScoped<IRequestHandler<FlumeRequest, string>, FlumeHandler>();
        var flumeProvider = flumeServices.BuildServiceProvider();
        _flume = flumeProvider.GetRequiredService<IMediator>();

        _mediatRRequest = new("test message");
        _flumeRequest = new("test message");
    }

    [Benchmark]
    public async Task<string> MediatRMemoryAllocation()
    {
        return await _mediatR.Send(_mediatRRequest);
    }

    [Benchmark]
    public async Task<string> FlumeMemoryAllocation()
    {
        return await _flume.Send(_flumeRequest);
    }

    [Benchmark]
    public async Task<string> MediatRMemoryAllocationWithMultipleRequests()
    {
        var results = new string[100];
        for (int i = 0; i < 100; i++)
        {
            results[i] = await _mediatR.Send(_mediatRRequest);
        }
        return results[0]; // Return first result for consistency
    }

    [Benchmark]
    public async Task<string> FlumeMemoryAllocationWithMultipleRequests()
    {
        var results = new string[100];
        for (int i = 0; i < 100; i++)
        {
            results[i] = await _flume.Send(_flumeRequest);
        }
        return results[0]; // Return first result for consistency
    }

    [Benchmark]
    public async Task<string> MediatRMemoryAllocationConcurrent()
    {
        var tasks = new Task<string>[50];
        for (int i = 0; i < 50; i++)
        {
            tasks[i] = _mediatR.Send(_mediatRRequest);
        }
        var results = await Task.WhenAll(tasks);
        return results[0]; // Return first result for consistency
    }

    [Benchmark]
    public async Task<string> FlumeMemoryAllocationConcurrent()
    {
        var tasks = new Task<string>[50];
        for (int i = 0; i < 50; i++)
        {
            tasks[i] = _flume.Send(_flumeRequest);
        }
        var results = await Task.WhenAll(tasks);
        return results[0]; // Return first result for consistency
    }
}
