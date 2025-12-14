using CDC.Application.Interfaces;
using CDC.Domain.Entities;
using CDC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CDC.Infrastructure.Configuration;

public class RoutingConfigurationService : IRoutingConfigurationService
{
    private readonly CdcDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "routing:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public RoutingConfigurationService(CdcDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<RoutingConfiguration?> GetRoutingConfigurationAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{tableName}";

        if (_cache.TryGetValue(cacheKey, out RoutingConfiguration? cachedConfig))
        { 
            return cachedConfig;
        }

        var config = await _context.RoutingConfigurations
            .FirstOrDefaultAsync(r => r.TableName == tableName && r.IsActive, cancellationToken);

        if (config != null)
        {
            _cache.Set(cacheKey, config, CacheDuration);
        }

        return config;
    }

    public async Task<List<RoutingConfiguration>> GetAllActiveConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RoutingConfigurations
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);
    }
}
