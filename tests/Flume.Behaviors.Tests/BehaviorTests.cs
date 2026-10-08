using System.Text;
using System.Text.RegularExpressions;
using Flume.Behaviors.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Flume.Behaviors.Tests;

public sealed class BehaviorTests
{
    [Fact]
    public async Task PerformanceBehaviourDoesNotLogWhenTheRequestIsFast()
    {
        var logger = new CollectingLogger<FastRequest>();
        var behavior = new PerformanceBehaviour<FastRequest, string>(logger);

        var response = await behavior.Handle(
            new FastRequest(),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", response);
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public async Task PerformanceBehaviourLogsTotalElapsedMilliseconds()
    {
        var logger = new CollectingLogger<SlowRequest>();
        var behavior = new PerformanceBehaviour<SlowRequest, string>(logger);

        var response = await behavior.Handle(
            new SlowRequest(),
            async cancellationToken =>
            {
                await Task.Delay(1200, cancellationToken);
                return "done";
            },
            CancellationToken.None);

        Assert.Equal("done", response);
        var elapsed = Assert.Single(logger.Messages.Select(ParseElapsedMilliseconds));
        Assert.True(elapsed >= 1000, $"Expected total elapsed milliseconds, got {elapsed}.");
    }

    [Fact]
    public async Task CacheResultStoresCompactJsonAndReusesIt()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var behavior = new CacheResultBehaviour<CachedRequest, CachedBody>(cache);
        var calls = 0;

        Task<CachedBody> Next(CancellationToken _)
        {
            calls++;
            return Task.FromResult(new CachedBody("hello"));
        }

        var first = await behavior.Handle(new CachedRequest(), Next, CancellationToken.None);
        var second = await behavior.Handle(new CachedRequest(), Next, CancellationToken.None);

        Assert.Equal("hello", first.Name);
        Assert.Equal("hello", second.Name);
        Assert.Equal(1, calls);

        var bytes = await cache.GetAsync("PAYLOAD");
        var json = Encoding.UTF8.GetString(bytes!);
        Assert.Equal("{\"Name\":\"hello\"}", json);
    }

    private static long ParseElapsedMilliseconds(string message)
    {
        var match = Regex.Match(message, @"\((\d+) milliseconds\)");
        Assert.True(match.Success, message);
        return long.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [PerformanceCheck(60_000)]
    private sealed record FastRequest : IRequest<string>;

    [PerformanceCheck(1)]
    private sealed record SlowRequest : IRequest<string>;

    [CacheResult("payload")]
    private sealed record CachedRequest : IRequest<CachedBody>;

    private sealed record CachedBody(string Name);

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}