using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Flume.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flume.Tests;

public sealed class CustomRequestTests
{
    [Fact]
    public async Task SendCustomRequestReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();
        services.AddScoped<IRequestHandler<CustomRequest, CustomResponse>, CustomRequestHandler>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var value1 = RandomNumberGenerator.GetInt32(0, 100);
        var value2 = RandomNumberGenerator.GetInt32(0, 100);

        // Act
        var result = await mediator.Send(new CustomRequest(value1, value2));
        
        // Assert
        Assert.Equal(value1 + value2, result.Value);
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
