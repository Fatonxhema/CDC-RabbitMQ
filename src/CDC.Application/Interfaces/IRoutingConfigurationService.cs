using CDC.Domain.Entities;

namespace CDC.Application.Interfaces;

public interface IRoutingConfigurationService
{
    Task<RoutingConfiguration?> GetRoutingConfigurationAsync(string tableName, CancellationToken cancellationToken = default);
    Task<List<RoutingConfiguration>> GetAllActiveConfigurationsAsync(CancellationToken cancellationToken = default);
}
