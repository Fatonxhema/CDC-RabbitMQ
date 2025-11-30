

using CDC.Application.DTOs;

namespace CDC.Application.Interfaces;

public interface ISequenceManager
{
    Task<bool> ValidateSequenceAsync(string partitionKey, long sequenceNumber, CancellationToken cancellationToken = default);
    Task UpdateSequenceAsync(string partitionKey, long sequenceNumber, CancellationToken cancellationToken = default);
    Task<long> GetExpectedSequenceAsync(string partitionKey, CancellationToken cancellationToken = default);
    Task BufferMessageAsync(string partitionKey, long sequenceNumber, CdcMessageDto message, CancellationToken cancellationToken = default);
    Task<List<CdcMessageDto>> GetBufferedMessagesAsync(string partitionKey, CancellationToken cancellationToken = default);
}
