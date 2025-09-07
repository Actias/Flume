using System.Reflection;
using Flume.Behaviors.Attributes;
using Flume.Behaviors.Extensions;
using Microsoft.Extensions.Caching.Distributed;

#pragma warning disable CA1812

namespace Flume.Behaviors;

public sealed class CacheResultBehaviour<TRequest, TResponse>(IDistributedCache cache)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attribute = typeof(TRequest).GetCustomAttribute<CacheResultAttribute>();

        if (attribute == null)
        {
            return await next(cancellationToken);
        }

        string cacheKey;

        if (attribute.Type == CacheResultType.CacheByName)
        {
            cacheKey = attribute.CacheKey;
        }
        else
        {
            var property = typeof(TRequest)
                .GetProperties()
                .ToList()
                .Find(x => x.Name.Equals(attribute.CacheKeyProperty, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                return await next(cancellationToken);
            }

            var value = property.GetValue(request);

            cacheKey = $"{attribute.CacheKey}:{value}";
        }

        var response = await cache.GetAsync<TResponse>(cacheKey, cancellationToken);

        if (response is not null)
        {
            return response;
        }

        response = await next(cancellationToken);

        await cache.SetAsync(
                cacheKey,
                response,
                new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(attribute.ExpirationInMinutes)
                },
                cancellationToken)
            ;

        return response;
    }
}
