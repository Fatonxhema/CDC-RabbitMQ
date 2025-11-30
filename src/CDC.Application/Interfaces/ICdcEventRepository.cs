using CDC.Domain.Entities;

namespace CDC.Application.Interfaces;

public interface ICdcEventRepository
{
    Task<CdcEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CdcEvent?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task AddAsync(CdcEvent cdcEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(CdcEvent cdcEvent, CancellationToken cancellationToken = default);
    Task<List<CdcEvent>> GetPendingRetries(int batchSize, CancellationToken cancellationToken = default);
}
