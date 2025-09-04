using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flume.Tests;

public sealed class AsyncRequestTests
{
    [Fact]
    public async Task SendAsyncRequestReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();
        services.AddScoped<IRequestHandler<AsyncRequest, int>, AsyncRequestHandler>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var value = RandomNumberGenerator.GetInt32(0, 100);

        // Act
        var result = await mediator.Send(new AsyncRequest(value));
        
        // Assert
        Assert.Equal(value, result);
    }


    [Fact]
    public async Task SendNullRequestThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send((SyncRequest)null!));
    }
}
