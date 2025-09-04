using System;
using Xunit;

namespace Flume.Tests;

public class SharedTests
{
    [Fact]
    public void ConstructorNullServiceProviderThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mediator(null!));
    }
}
