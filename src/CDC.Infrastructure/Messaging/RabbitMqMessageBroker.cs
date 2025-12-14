using CDC.Application.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CDC.Infrastructure.Messaging;

public class RabbitMqMessageBroker : IMessageBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBroker> _logger;

    public RabbitMqMessageBroker(IConnection connection, ILogger<RabbitMqMessageBroker> logger)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _channel.ConfirmSelect();
        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, true);

        _channel.BasicPublish(exchange, routingKey, properties, body);

        _logger.LogDebug("Published message to exchange {Exchange} with routing key {RoutingKey}", exchange, routingKey);

        await Task.CompletedTask;
    }

    public async Task<bool> PublishWithConfirmationAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(exchange, routingKey, message, cancellationToken);

        try
        {
            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm message publication");
            return false;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
