using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CDC.Listener.API;

public class ListenerBackgroundService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<ListenerBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private IModel? _channel;

    public ListenerBackgroundService(
        IConnection connection,
        ILogger<ListenerBackgroundService> logger,
        IConfiguration configuration)
    {
        _connection = connection;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        
        // Declare queues for different operations/tables
        var queues = new[] { "customer.events", "order.events", "product.events" };
        
        foreach (var queue in queues)
        {
            _channel.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Received message on queue {Queue}: {Message}", 
                    ea.RoutingKey, message);

                // Simulate processing
                await Task.Delay(100, stoppingToken);

                // Acknowledge successful processing
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        foreach (var queue in queues)
        {
            _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
