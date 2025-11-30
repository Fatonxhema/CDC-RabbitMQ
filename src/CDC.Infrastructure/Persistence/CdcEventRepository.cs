using CDC.Application.Interfaces;
using CDC.Domain.Entities;
using CDC.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CDC.Infrastructure.Persistence;

public class CdcEventRepository : ICdcEventRepository
{
    private readonly CdcDbContext _context;

    public CdcEventRepository(CdcDbContext context)
    {
        _context = context;
    }

    public async Task<CdcEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CdcEvents.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<CdcEvent?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.CdcEvents
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(CdcEvent cdcEvent, CancellationToken cancellationToken = default)
    {
        await _context.CdcEvents.AddAsync(cdcEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CdcEvent cdcEvent, CancellationToken cancellationToken = default)
    {
        _context.CdcEvents.Update(cdcEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CdcEvent>> GetPendingRetries(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.CdcEvents
            .Where(e => e.Status == ProcessingStatus.RetryScheduled.ToString() || e.Status == ProcessingStatus.Failed.ToString())
            .Where(e => e.RetryCount < 5)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
