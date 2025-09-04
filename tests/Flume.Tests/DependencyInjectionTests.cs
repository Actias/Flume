using System.Security.Cryptography;
using System.Threading.Tasks;
using Flume.Tests.Requests;
using Flume.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flume.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task SendSyncServiceRequestReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();
        services.AddScoped<IRequestHandler<AsyncServiceRequest, int>, AsyncServiceRequestHandler>();
        services.AddScoped<IMockService, MockService>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var value = RandomNumberGenerator.GetInt32(0, 100);

        // Act
        var result = await mediator.Send(new AsyncServiceRequest(value));

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SendAsyncServiceRequestReturnsExpectedResult()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddFlume();
        services.AddScoped<IRequestHandler<SyncServiceRequest, int>, SyncServiceRequestHandler>();
        services.AddScoped<IMockService, MockService>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var value = RandomNumberGenerator.GetInt32(0, 100);

        // Act
        var result = await mediator.Send(new SyncServiceRequest(value));
        
        // Assert
        Assert.Equal(value, result);
    }

}
