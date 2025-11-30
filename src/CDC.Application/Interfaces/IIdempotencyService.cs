namespace CDC.Application.Interfaces;

public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string messageId, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}
