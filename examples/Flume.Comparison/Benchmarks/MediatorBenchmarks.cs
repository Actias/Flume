using BenchmarkDotNet.Attributes;
using Flume.Comparison.Flume;
using Flume.Comparison.MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Flume.Comparison.Benchmarks;

/// <summary>
/// MediatR 12.5.0 against Flume on the current TFM.
/// Includes a reused mediator, a new mediator per iteration, and one real pass-through behavior.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class MediatorBenchmarks
{
    private ServiceProvider _mediatRProvider = null!;
    private ServiceProvider _flumeProvider = null!;
    private ServiceProvider _mediatRPipelineProvider = null!;
    private ServiceProvider _flumePipelineProvider = null!;
    private global::MediatR.IMediator _mediatR = null!;
    private IMediator _flume = null!;
    private global::MediatR.IMediator _mediatRPipeline = null!;
    private IMediator _flumePipeline = null!;
    private MediatRRequest _mediatRRequest = null!;
    private FlumeRequest _flumeRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediatRProvider = BuildMediatR(includeBehavior: false);
        _flumeProvider = BuildFlume(includeBehavior: false);
        _mediatRPipelineProvider = BuildMediatR(includeBehavior: true);
        _flumePipelineProvider = BuildFlume(includeBehavior: true);

        _mediatR = _mediatRProvider.GetRequiredService<global::MediatR.IMediator>();
        _flume = _flumeProvider.GetRequiredService<IMediator>();
        _mediatRPipeline = _mediatRPipelineProvider.GetRequiredService<global::MediatR.IMediator>();
        _flumePipeline = _flumePipelineProvider.GetRequiredService<IMediator>();
        _mediatRRequest = new("test message");
        _flumeRequest = new("test message");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _mediatRProvider.Dispose();
        _flumeProvider.Dispose();
        _mediatRPipelineProvider.Dispose();
        _flumePipelineProvider.Dispose();
    }

    [Benchmark]
    public Task<string> MediatRSend() => _mediatR.Send(_mediatRRequest);

    [Benchmark]
    public Task<string> FlumeSend() => _flume.Send(_flumeRequest);

    [Benchmark]
    public Task<string> MediatRSendFreshMediator() =>
        _mediatRProvider.GetRequiredService<global::MediatR.IMediator>().Send(_mediatRRequest);

    [Benchmark]
    public Task<string> FlumeSendFreshMediator() =>
        _flumeProvider.GetRequiredService<IMediator>().Send(_flumeRequest);

    [Benchmark]
    public Task<string> MediatRSendWithBehavior() => _mediatRPipeline.Send(_mediatRRequest);

    [Benchmark]
    public Task<string> FlumeSendWithBehavior() => _flumePipeline.Send(_flumeRequest);

    private static ServiceProvider BuildMediatR(bool includeBehavior)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.TypeEvaluator = type => type.DeclaringType != typeof(MediatorBenchmarks);
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            if (includeBehavior)
            {
                cfg.AddOpenBehavior(typeof(MediatRPassThroughBehavior<,>));
            }
        });

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildFlume(bool includeBehavior)
    {
        var services = new ServiceCollection();
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = type => type.DeclaringType != typeof(MediatorBenchmarks);
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            if (includeBehavior)
            {
                cfg.AddOpenBehavior(typeof(FlumePassThroughBehavior<,>));
            }
        });

        return services.BuildServiceProvider();
    }

    public sealed class MediatRPassThroughBehavior<TRequest, TResponse> : global::MediatR.IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(
            TRequest request,
            global::MediatR.RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) =>
            next(cancellationToken);
    }

    public sealed class FlumePassThroughBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) =>
            next(cancellationToken);
    }
}