using CDC.Application.Interfaces;
using CDC.Domain.Enums;
using Polly;
using Polly.Retry;

namespace CDC.RetryProcessor;

public class RetryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetryWorker> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    public RetryWorker(IServiceProvider serviceProvider, ILogger<RetryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retry Processor starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var eventRepository = scope.ServiceProvider.GetRequiredService<ICdcEventRepository>();
                var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
                var routingService = scope.ServiceProvider.GetRequiredService<IRoutingConfigurationService>();

                var pendingEvents = await eventRepository.GetPendingRetries(10, stoppingToken);

                foreach (var evt in pendingEvents)
                {
                    try
                    {
                        var routingConfig = await routingService.GetRoutingConfigurationAsync(evt.TableName, stoppingToken);

                        if (routingConfig != null)
                        {
                            await _retryPipeline.ExecuteAsync(async ct =>
                            {
                                await messageBroker.PublishWithConfirmationAsync(
                                    routingConfig.Exchange,
                                    routingConfig.RoutingKey,
                                    new { evt.MessageId, evt.TableName, evt.Payload },
                                    ct);
                            }, stoppingToken);

                            evt.Status = ProcessingStatus.Completed.ToString();
                            evt.ProcessedAt = DateTime.UtcNow;
                            _logger.LogInformation("Retry successful for message {MessageId}", evt.MessageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Retry failed for message {MessageId}", evt.MessageId);
                        evt.RetryCount++;

                        if (evt.RetryCount >= 5)
                        {
                            evt.Status = ProcessingStatus.DeadLettered.ToString();
                        }
                        else
                        {
                            evt.Status = ProcessingStatus.RetryScheduled.ToString();
                        }

                        evt.ErrorMessage = ex.Message;
                    }

                    await eventRepository.UpdateAsync(evt, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retry processor");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
