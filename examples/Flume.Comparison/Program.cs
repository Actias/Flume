using System.Diagnostics;
using BenchmarkDotNet.Running;
using Flume.Comparison.Benchmarks;
using Flume.Comparison.Flume;
using Flume.Comparison.MediatR;
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
        
        Console.WriteLine("\nRunning memory allocation benchmarks...");
        
        BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
        
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

        var flumeServices = new ServiceCollection();
        flumeServices.AddFlume(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        var flumeProvider = flumeServices.BuildServiceProvider();
        var flume = flumeProvider.GetRequiredService<IMediator>();

        var flumeRequest = new FlumeRequest("Hello from Flume!");
        var flumeResult = await flume.Send(flumeRequest);

        Console.WriteLine($"Result: {flumeResult}");

        // Performance comparison with stopwatch
        Console.WriteLine("\n--- Quick Performance Test #1 - 100 ---");

        await QuickPerformanceTest(mediatR, flume, 100);

        Console.WriteLine("\n--- Quick Performance Test #2 - 1000 ---");

        await QuickPerformanceTest(mediatR, flume, 1000);

        Console.WriteLine("\n--- Quick Performance Test #3 - 10000 ---");

        await QuickPerformanceTest(mediatR, flume, 10000);

        Console.WriteLine("\n--- Quick Performance Test #4 - 100000 ---");

        await QuickPerformanceTest(mediatR, flume, 100000);

        Console.WriteLine("\n--- Quick Performance Test #5 - 100000 ---");

        await QuickPerformanceTest(mediatR, flume, 100000);

        Console.WriteLine("\n--- Quick Performance Test #6 - 1000000 ---");

        await QuickPerformanceTest(mediatR, flume, 1000000);
    }

    private static async Task QuickPerformanceTest(global::MediatR.IMediator mediatR, IMediator flume, int iterations)
    {
        // Test MediatR
        var mediatRStart = Stopwatch.GetTimestamp();

        for (var i = 0; i < iterations; i++)
        {
            await mediatR.Send(new MediatRRequest($"Message {i}"));
        }

        var mediatREnd = Stopwatch.GetTimestamp();

        // Test Flume
        var flumeStart = Stopwatch.GetTimestamp();

        for (var i = 0; i < iterations; i++)
        {
            await flume.Send(new FlumeRequest($"Message {i}"));
        }

        var flumeEnd = Stopwatch.GetTimestamp();

        var mediatrTime = Stopwatch.GetElapsedTime(mediatRStart, mediatREnd);
        var flumeTime = Stopwatch.GetElapsedTime(flumeStart, flumeEnd);
        
        Console.WriteLine($"MediatR: {mediatrTime.Ticks} ticks for {iterations} requests.");
        Console.WriteLine($"Flume: {flumeTime.Ticks} ticks for {iterations} requests.");

        if (mediatrTime.Ticks < flumeTime.Ticks)
        {
            Console.WriteLine($"MediatR is {(double)flumeTime.Ticks / mediatrTime.Ticks:F2}x faster");
        }

        if (mediatrTime.Ticks > flumeTime.Ticks)
        {
            Console.WriteLine($"Flume is {(double)mediatrTime.Ticks / flumeTime.Ticks:F2}x faster");
        }

        if (mediatrTime.Ticks == flumeTime.Ticks)
        {
            Console.WriteLine("Both are equal");
        }
    }
}
