using System.Threading.Tasks;
using Flume.Handlers;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CA1707 // Underscores in test names for readability

namespace Flume.Tests;

public class RequestResponseTests
{
    [Fact]
    public async Task Send_RequestWithResponse_ReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlume();
        services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new Ping());

        // Assert
        Assert.Equal("Pong", result);
    }

    [Fact]
    public async Task Send_RequestWithoutResponse_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlume();
        services.AddScoped<IRequestHandler<Pong, Unit>, PongHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var result = await mediator.Send(new Pong()); // Should not throw

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Send_ObjectRequest_ReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();
        services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send((object)new Ping());

        // Assert
        Assert.Equal("Pong", result);
    }


}
