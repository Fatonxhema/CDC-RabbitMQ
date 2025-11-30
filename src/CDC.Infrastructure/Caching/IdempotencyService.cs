using CDC.Application.Interfaces;

namespace CDC.Infrastructure.Caching;

public class IdempotencyService : IIdempotencyService
{
    private readonly ICacheService _cacheService;
    private const string IdempotencyPrefix = "processed:";

    public IdempotencyService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var key = $"{IdempotencyPrefix}{messageId}";
        var value = await _cacheService.GetAsync<bool?>(key, cancellationToken);
        return value.HasValue && value.Value;
    }

    public async Task MarkAsProcessedAsync(string messageId, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var key = $"{IdempotencyPrefix}{messageId}";
        expiration ??= TimeSpan.FromDays(7);
        await _cacheService.SetAsync(key, true, expiration, cancellationToken);
    }
}
