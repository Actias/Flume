using System.Diagnostics;
using System.Threading.Tasks;
using Flume.Handlers;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CA1707 // Underscores in test names for readability

namespace Flume.Tests;

public class DelayResponseTests
{
    [Fact]
    public async Task Send_BoingRequest_ReturnsCorrectSumAfterDelay()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlume();
        services.AddScoped<IRequestHandler<Boing, BoingResponse>, BoingHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new Boing { Thing1 = 5, Thing2 = 7 };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await mediator.Send(request);
        stopwatch.Stop();

        // Assert
        Assert.Equal(12, result.Sum);
        Assert.True(stopwatch.ElapsedMilliseconds >= 1000, "Should have delayed for at least 1 second");
        Assert.True(stopwatch.ElapsedMilliseconds <= 5000, "Should have delayed for no more than 4 seconds");
    }

    [Fact]
    public async Task Send_BoingRequest_WithDifferentValues_ReturnsCorrectSum()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlume();
        services.AddScoped<IRequestHandler<Boing, BoingResponse>, BoingHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new Boing { Thing1 = 10, Thing2 = 20 };

        // Act
        var result = await mediator.Send(request);

        // Assert
        Assert.Equal(30, result.Sum);
    }

    [Fact]
    public async Task Send_BoingRequest_WithNegativeValues_ReturnsCorrectSum()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlume();
        services.AddScoped<IRequestHandler<Boing, BoingResponse>, BoingHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new Boing { Thing1 = -5, Thing2 = 3 };

        // Act
        var result = await mediator.Send(request);

        // Assert
        Assert.Equal(-2, result.Sum);
    }
}
