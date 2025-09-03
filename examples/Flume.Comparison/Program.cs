using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Running;
using Flume.Comparison.Benchmarks;
using Flume.Comparison.MediatR;
using Flume.Comparison.SimpleMediator;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1303 // Localization not needed for console output

namespace Flume.Comparison;

public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Flume vs MediatR 12.5.0 Comparison ===\n");

        // Feature comparison
        await DemonstrateFeatures();

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // Performance comparison
        Console.WriteLine("Running performance benchmarks...");

        BenchmarkRunner.Run<MediatorBenchmarks>();
        
        Console.WriteLine("\nBenchmark completed! Check the results above.");
    }

    private static async Task DemonstrateFeatures()
    {
        Console.WriteLine("1. FEATURE COMPARISON");
        Console.WriteLine("=====================");

        // MediatR demonstration
        Console.WriteLine("\n--- MediatR 12.5.0 ---");

        var mediatRServices = new ServiceCollection();

        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        var mediatRProvider = mediatRServices.BuildServiceProvider();
        var mediatR = mediatRProvider.GetRequiredService<global::MediatR.IMediator>();

        var mediatRRequest = new MediatRRequest("Hello from MediatR!");
        var mediatRResult = await mediatR.Send(mediatRRequest);

        Console.WriteLine($"Result: {mediatRResult}");

        // Flume demonstration
        Console.WriteLine("\n--- Flume ---");

        var simpleServices = new ServiceCollection();

        simpleServices.AddFlume(Assembly.GetExecutingAssembly());

        var simpleProvider = simpleServices.BuildServiceProvider();
        var flume = simpleProvider.GetRequiredService<IMediator>();

        var simpleRequest = new FlumeRequest("Hello from Flume!");
        var simpleResult = await flume.Send(simpleRequest);

        Console.WriteLine($"Result: {simpleResult}");

        // Performance comparison with stopwatch
        Console.WriteLine("\n--- Quick Performance Test ---");

        await QuickPerformanceTest(mediatR, flume);
    }

    private static async Task QuickPerformanceTest(global::MediatR.IMediator mediatR, IMediator flume)
    {
        const int iterations = 10000;
        
        // Test MediatR
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            await mediatR.Send(new MediatRRequest($"Message {i}"));
        }
        
        stopwatch.Stop();
        
        var mediatRTime = stopwatch.ElapsedTicks;

        // Test Flume
        stopwatch.Restart();
        
        for (int i = 0; i < iterations; i++)
        {
            await flume.Send(new FlumeRequest($"Message {i}"));
        }

        stopwatch.Stop();

        var flumeTime = stopwatch.ElapsedTicks;

        Console.WriteLine($"MediatR: {mediatRTime} ticks for {iterations} requests");
        Console.WriteLine($"Flume: {flumeTime} ticks for {iterations} requests");
        Console.WriteLine($"Flume is {(double)mediatRTime / flumeTime:F2}x faster");
    }
}
