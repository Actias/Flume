using Flume.Behaviors.Exceptions;

namespace Flume.Behaviors.Attributes;

public enum CacheResultType
{
    CacheByName,
    CacheByProperty
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheResultAttribute(
    string cacheKey,
    int expirationInMinutes = 10,
    CacheResultType type = CacheResultType.CacheByName,
    string cacheKeyProperty = "") : Attribute
{
    public string CacheKey { get; } = string.IsNullOrWhiteSpace(cacheKey)
            ? throw new ConfigurationErrorException("CacheKey is required to use inline caching.")
            : cacheKey.ToUpperInvariant();

    public string CacheKeyProperty { get; } = string.IsNullOrWhiteSpace(cacheKeyProperty) && type == CacheResultType.CacheByProperty
            ? throw new ConfigurationErrorException("CacheKeyProperty is required to use inline caching by property.")
            : cacheKeyProperty.ToUpperInvariant();

    public int ExpirationInMinutes { get; } = expirationInMinutes;

    public CacheResultType Type { get; } = type;
}
