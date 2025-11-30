using CDC.Application.DTOs;
using CDC.Application.Interfaces;

namespace CDC.Infrastructure.Caching;

public class SequenceManager : ISequenceManager
{
    private readonly ICacheService _cacheService;
    private const string SequencePrefix = "seq:";
    private const string BufferPrefix = "buffer:";

    public SequenceManager(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<bool> ValidateSequenceAsync(string partitionKey, long sequenceNumber, CancellationToken cancellationToken = default)
    {
        var expectedSequence = await GetExpectedSequenceAsync(partitionKey, cancellationToken);
        return sequenceNumber == expectedSequence;
    }

    public async Task UpdateSequenceAsync(string partitionKey, long sequenceNumber, CancellationToken cancellationToken = default)
    {
        var key = $"{SequencePrefix}{partitionKey}";
        await _cacheService.SetAsync(key, sequenceNumber + 1, cancellationToken: cancellationToken);
    }

    public async Task<long> GetExpectedSequenceAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        var key = $"{SequencePrefix}{partitionKey}";
        var sequence = await _cacheService.GetAsync<long?>(key, cancellationToken);
        return sequence ?? 0;
    }

    public async Task BufferMessageAsync(string partitionKey, long sequenceNumber, CdcMessageDto message, CancellationToken cancellationToken = default)
    {
        var key = $"{BufferPrefix}{partitionKey}:{sequenceNumber}";
        await _cacheService.SetAsync(key, message, TimeSpan.FromHours(1), cancellationToken);
    }

    public async Task<List<CdcMessageDto>> GetBufferedMessagesAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        var messages = new List<CdcMessageDto>();
        
        for (long i = 0; i < 100; i++)
        {
            var key = $"{BufferPrefix}{partitionKey}:{i}";
            var message = await _cacheService.GetAsync<CdcMessageDto>(key, cancellationToken);
            if (message != null)
            {
                messages.Add(message);
                await _cacheService.DeleteAsync(key, cancellationToken);
            }
        }

        return messages;
    }
}
