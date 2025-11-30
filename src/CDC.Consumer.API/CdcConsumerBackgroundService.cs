using CDC.Application.DTOs;
using CDC.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CDC.Consumer.API;

public class CdcConsumerBackgroundService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CdcConsumerBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private IModel? _channel;

    public CdcConsumerBackgroundService(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<CdcConsumerBackgroundService> logger,
        IConfiguration configuration)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();

        var queueName = _configuration["RabbitMQ:CdcQueue"] ?? "cdc.events";

        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var cdcMessage = JsonSerializer.Deserialize<CdcMessageDto>(message);

                if (cdcMessage != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processingService = scope.ServiceProvider.GetRequiredService<CdcProcessingService>();

                    await processingService.ProcessCdcEventAsync(cdcMessage, stoppingToken);

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Processed message {MessageId}", cdcMessage.MessageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CDC event");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
