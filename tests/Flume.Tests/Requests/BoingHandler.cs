using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Flume.Handlers;

namespace Flume.Tests.Requests;

public class BoingHandler : IRequestHandler<Boing, BoingResponse>
{
    public async Task<BoingResponse> Handle(Boing request, CancellationToken cancellationToken = default)
    {
        var delaySeconds = RandomNumberGenerator.GetInt32(1, 5); // Random delay between 1-4 seconds
        
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        
        return new()
        {
            Sum = request.Thing1 + request.Thing2
        };
    }
}
