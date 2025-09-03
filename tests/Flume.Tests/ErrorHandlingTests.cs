using System;
using System.Threading.Tasks;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CA1707 // Underscores in test names for readability

namespace Flume.Tests;

public class ErrorHandlingTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mediator(null!));
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send((Ping)null!));
    }

    [Fact]
    public async Task Publish_NullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Publish((Pinged)null!));
    }
}
