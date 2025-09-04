#pragma warning disable CA1303 // Localization not needed for console output

namespace Flume.Samples;

// Example request and response
public class Ping : IRequest<string> { }

public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Pong");
    }
}

// Example request without response
public class Pong : IRequest<Unit> { }

public class PongHandler : IRequestHandler<Pong, Unit>
{
    public Task<Unit> Handle(Pong request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Pong handled!");
        
        return Task.FromResult(Unit.Value);
    }
}

// Example notification
public class Pinged : INotification { }

public class PingedHandler : INotificationHandler<Pinged>
{
    public Task Handle(Pinged notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Pinged notification handled!");

        return Task.CompletedTask;
    }
}
