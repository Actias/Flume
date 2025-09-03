using BenchmarkDotNet.Attributes;
using Flume.Comparison.MediatR;
using Flume.Comparison.SimpleMediator;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Comparison.Benchmarks;

/// <summary>
/// Benchmark class for performance comparison between MediatR and Flume
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
        var services = new ServiceCollection();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));

        var serviceProvider = services.BuildServiceProvider();

        _mediatR = serviceProvider.GetRequiredService<global::MediatR.IMediator>();

        // Setup Flume
        var simpleServices = new ServiceCollection();

        simpleServices.AddFlume();
        simpleServices.AddScoped<Handlers.IRequestHandler<FlumeRequest, string>, FlumeHandler>();

        var simpleServiceProvider = simpleServices.BuildServiceProvider();

        _flume = simpleServiceProvider.GetRequiredService<IMediator>();

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
}
