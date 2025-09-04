using System;
using System.Collections.Concurrent;

namespace Flume.Internal;

/// <summary>
/// Thread-safe object pool for frequently created objects to reduce GC pressure
/// </summary>
/// <typeparam name="T">Type of object to pool</typeparam>
public class ObjectPool<T>(int maxPoolSize = 100, Func<T>? factory = null) where T : class, new()
{
    private readonly ConcurrentQueue<T> _pool = new();
    private readonly Func<T> _factory = factory ?? (() => new());

    /// <summary>
    /// Rent an object from the pool or create a new one if the pool is empty
    /// </summary>
    public T Rent()
    {
        return _pool.TryDequeue(out var item) ? item : _factory();
    }
    
    /// <summary>
    /// Return an object to the pool for reuse
    /// </summary>
    public void Return(T item)
    {
        if (item != null && _pool.Count < maxPoolSize)
        {
            _pool.Enqueue(item);
        }
    }
    
    /// <summary>
    /// Clear all objects from the pool
    /// </summary>
    public void Clear()
    {
        while (_pool.TryDequeue(out _))
        {
            // Intentionally empty - just clearing the queue
        }
    }
    
    /// <summary>
    /// Get the current number of objects in the pool
    /// </summary>
    public int Count => _pool.Count;
}
