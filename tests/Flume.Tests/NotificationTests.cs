using System.Threading.Tasks;
using Flume.Handlers;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CA1707 // Underscores in test names for readability

namespace Flume.Tests;

public class NotificationTests
{
    [Fact]
    public async Task Publish_Notification_HandledSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlume();
        services.AddScoped<INotificationHandler<Pinged>, PingedHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await mediator.Publish(new Pinged()); // Should not throw

        // Success is defined as no exception being thrown
        Assert.True(true, "Notification published successfully without exceptions");
    }
}
