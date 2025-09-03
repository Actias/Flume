using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace Flume.Behaviors.Extensions;

internal static class DistributedCacheExtensions
{
    public static Task SetAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        return SetAsync(cache, key, value, new(), cancellationToken);
    }

    public static Task SetAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, GetJsonSerializerOptions()));

        return cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public static async Task<T?> GetAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        var value = bytes == null ? default : JsonSerializer.Deserialize<T>(bytes, GetJsonSerializerOptions());

        return value ?? default;
    }

    public static async Task<T?> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        return await GetOrSetAsync(cache, key, factory, new(), cancellationToken);
    }

    public static async Task<T?> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<Task<T>> factory,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        var value = await cache.GetAsync<T>(key, cancellationToken);

        if (value is not null)
        {
            return value;
        }

        value = await factory();
        
        await cache.SetAsync(key, value, options, cancellationToken);

        return value;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new()
        {
            PropertyNamingPolicy = null,
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
