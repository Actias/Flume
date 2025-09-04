using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Flume.Internal;

/// <summary>
/// Lock-free cache implementation using concurrent collections for better performance
/// </summary>
/// <typeparam name="TKey">Cache key type</typeparam>
/// <typeparam name="TValue">Cache value type</typeparam>
public class LockFreeCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _cache;
    private readonly ConcurrentQueue<TKey> _evictionQueue;
    private readonly int _maxSize;
    
    /// <summary>
    /// Initializes a new instance of the LockFreeCache class
    /// </summary>
    /// <param name="maxSize">Maximum number of items in the cache</param>
    /// <param name="comparer">Optional equality comparer for keys</param>
    public LockFreeCache(int maxSize = 1000, IEqualityComparer<TKey>? comparer = null)
    {
        _maxSize = maxSize;
        var keyComparer = comparer ?? EqualityComparer<TKey>.Default;
        _cache = new(keyComparer);
        _evictionQueue = new();
    }
    
    /// <summary>
    /// Get or add a value to the cache
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        var result = _cache.GetOrAdd(key, factory);
        
        // Lock-free eviction when cache gets too large
        if (_cache.Count > _maxSize)
        {
            TryEvictOldest();
        }
        
        return result;
    }
    
    /// <summary>
    /// Try to get a value from the cache
    /// </summary>
    public bool TryGet(TKey key, out TValue? value)
    {
        return _cache.TryGetValue(key, out value);
    }
    
    /// <summary>
    /// Add or update a value in the cache
    /// </summary>
    public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateFactory)
    {
        return _cache.AddOrUpdate(key, value, updateFactory);
    }
    
    /// <summary>
    /// Remove a key from the cache
    /// </summary>
    public bool TryRemove(TKey key, out TValue? value)
    {
        return _cache.TryRemove(key, out value);
    }
    
    /// <summary>
    /// Clear all items from the cache
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        while (_evictionQueue.TryDequeue(out _))
        {
            // Intentionally empty - just clearing the queue
        }
    }
    
    /// <summary>
    /// Get the current number of items in the cache
    /// </summary>
    public int Count => _cache.Count;
    
    /// <summary>
    /// Get all keys in the cache
    /// </summary>
    public IEnumerable<TKey> Keys => _cache.Keys;
    
    /// <summary>
    /// Get all values in the cache
    /// </summary>
    public IEnumerable<TValue> Values => _cache.Values;
    
    private void TryEvictOldest()
    {
        // Try to evict the oldest item in a lock-free manner
        if (_evictionQueue.TryDequeue(out var oldestKey))
        {
            _cache.TryRemove(oldestKey, out _);
        }
    }
    
    /// <summary>
    /// Add a key to the eviction queue for future removal
    /// </summary>
    public void EnqueueForEviction(TKey key)
    {
        _evictionQueue.Enqueue(key);
    }
}
