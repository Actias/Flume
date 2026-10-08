# Cache configuration

`CacheConfiguration`, `PerformanceStrategy`, and `CacheEvictionPolicy` were removed. They only described a handler cache that did not evict, and dependency injection never passed that configuration into `Mediator`.

The mediator keeps a static wrapper per closed request type. That cache is intentionally unbounded: an application has one entry per request type, and the entries do not hold scoped services. There is no TTL, LRU, or size setting on that cache.

For response caching, use `Flume.Behaviors` (`CacheResultBehaviour` and `IDistributedCache`).
