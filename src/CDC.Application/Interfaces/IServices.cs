
namespace CDC.Application.Interfaces;

public interface IMessageBroker
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
    Task<bool> PublishWithConfirmationAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
}
