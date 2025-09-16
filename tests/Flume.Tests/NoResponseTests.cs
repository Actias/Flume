using System.Threading.Tasks;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flume.Tests;

public sealed class NoResponseTests
{
    [Fact]
    public async Task RequestWithNoResponseSucceeds()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var exception = await Record.ExceptionAsync(() => mediator.Send(new NoResponseRequest()));

        // Assert
        // If we reach this point without exceptions, the test passes
        Assert.Null(exception);
    }
}