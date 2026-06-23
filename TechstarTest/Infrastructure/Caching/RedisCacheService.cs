using StackExchange.Redis;
using System.Text.Json;

namespace TechstarTest.Infrastructure.Caching;


public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);

    CacheMetrics GetCacheMetrics();

    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiry,
        CancellationToken ct = default);
}

public class CacheMetrics
{
    private long _hits;
    private long _misses;

    public void RecordHit() => Interlocked.Increment(ref _hits);
    public void RecordMiss() => Interlocked.Increment(ref _misses);

    public long Hits => Interlocked.Read(ref _hits);
    public long Misses => Interlocked.Read(ref _misses);
    public double HitRatio => (Hits + Misses) == 0 ? 0 : (double)Hits / (Hits + Misses);

    public override string ToString() =>
        $"Hits={Hits} | Misses={Misses} | HitRatio={HitRatio:P1}";
}
public class RedisCacheService: ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheMetrics _metrics = new();

    private const string LockPrefix = "lock:";
    private const int LockRetryCount = 10;
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(150);

    public RedisCacheService(IConnectionMultiplexer connection, ILogger<RedisCacheService> logger)
    {
        _db = connection.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            _metrics.RecordMiss();
            return default;
        }
        _metrics.RecordHit();
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    private async Task<T?> GetWithoutRecordingMissAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);

        if (value.IsNullOrEmpty) return default; 

        _metrics.RecordHit(); 
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public CacheMetrics GetCacheMetrics() => _metrics;

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for key {Key} | {Metrics}.", key, _metrics);
            return cached;
        }

        _logger.LogInformation("Cache miss for key {Key}.", key);

        var lockKey = $"{LockPrefix}{key}";
        var lockToken = Guid.NewGuid().ToString();
        for (int attempt = 0; attempt < LockRetryCount; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var acquired = await _db.StringSetAsync(
                lockKey, lockToken, LockExpiry, When.NotExists);

            if (acquired)
            {
                _logger.LogInformation("Lock acquisito for {Key} (attempt {Attempt})", key, attempt + 1);

                try
                {

                    var doubleCheck = await GetWithoutRecordingMissAsync<T>(key); 
                    if (doubleCheck is not null)
                    {
                        _logger.LogInformation("Cache hit for (double-check) → {Key}", key);
                        return doubleCheck;
                    }

                    var result = await factory(ct);
                    if (result is not null)
                    {
                        await SetAsync(key, result, expiry, ct);
                        _logger.LogInformation("Cache set for {Key} (TTL: {Expiry})", key, expiry);
                    }

                    return result;
                }
                finally
                {

                    var currentToken = await _db.StringGetAsync(lockKey);
                    if (currentToken == lockToken)
                        await _db.KeyDeleteAsync(lockKey);

                    _logger.LogInformation("Lock rilasciato → {Key}", key);
                }
            }
            _logger.LogInformation("Lock not acquired for {Key} (attempt {Attempt}/{Max}) — attendo {Delay}ms", key, attempt + 1, LockRetryCount, LockRetryDelay.TotalMilliseconds);
            await Task.Delay(LockRetryDelay, ct);

            var retryCheck = await GetWithoutRecordingMissAsync<T>(key);
            if (retryCheck is not null)
            {
                _logger.LogInformation("Cache hit for (retry-check) → {Key}", key);
                return retryCheck;
            }
        }
        
        _logger.LogWarning("Failed to acquire lock for {Key} fallback to DataBase.", key);
        return await factory(ct);
    }

}
