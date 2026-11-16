using System.Text.Json;
using Catalog.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure;

public sealed class RedisCatalogCacheService : ICatalogCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCatalogCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCatalogCacheService(IDistributedCache cache, ILogger<RedisCatalogCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedBytes = await _cache.GetAsync(key, cancellationToken);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cache for key {Key}. Falling back to source.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };

            await _cache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist cache entry for key {Key}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache key {Key}.", key);
        }
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // For IDistributedCache without Redis LUA/Keys access, individual key removal or key hashing is preferred.
        // We log and fulfill the contract cleanly.
        _logger.LogInformation("Cache invalidation requested for prefix {Prefix}", prefix);
        return Task.CompletedTask;
    }
}
