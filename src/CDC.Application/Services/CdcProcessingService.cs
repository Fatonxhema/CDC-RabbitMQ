using CDC.Application.DTOs;
using CDC.Application.Interfaces;
using CDC.Domain.Entities;
using CDC.Domain.Enums;
using CDC.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CDC.Application.Services;

public class CdcProcessingService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ISequenceManager _sequenceManager;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IRoutingConfigurationService _routingService;
    private readonly ICdcEventRepository _eventRepository;
    private readonly ILogger<CdcProcessingService> _logger;

    public CdcProcessingService(
        IMessageBroker messageBroker,
        ISequenceManager sequenceManager,
        IIdempotencyService idempotencyService,
        IRoutingConfigurationService routingService,
        ICdcEventRepository eventRepository,
        ILogger<CdcProcessingService> logger)
    {
        _messageBroker = messageBroker;
        _sequenceManager = sequenceManager;
        _idempotencyService = idempotencyService;
        _routingService = routingService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task ProcessCdcEventAsync(CdcMessageDto message, CancellationToken cancellationToken = default)
    {
        // Check idempotency
        if (await _idempotencyService.IsProcessedAsync(message.MessageId, cancellationToken))
        {
            _logger.LogInformation("Message {MessageId} already processed, skipping", message.MessageId);
            return;
        }

        // Validate and manage sequence
        var isSequenceValid = await _sequenceManager.ValidateSequenceAsync(
            message.PartitionKey,
            message.SequenceNumber,
            cancellationToken);

        if (!isSequenceValid)
        {
            _logger.LogWarning(
                "Sequence gap detected for partition {PartitionKey}. Buffering message {MessageId} with sequence {Sequence}",
                message.PartitionKey, message.MessageId, message.SequenceNumber);

            await _sequenceManager.BufferMessageAsync(
                message.PartitionKey,
                message.SequenceNumber,
                message,
                cancellationToken);
            return;
        }

        // Process the message
        await ProcessMessageAsync(message, cancellationToken);

        // Update sequence
        await _sequenceManager.UpdateSequenceAsync(
            message.PartitionKey,
            message.SequenceNumber,
            cancellationToken);

        // Mark as processed
        await _idempotencyService.MarkAsProcessedAsync(
            message.MessageId,
            TimeSpan.FromDays(7),
            cancellationToken);

        // Check for buffered messages that can now be processed
        await ProcessBufferedMessagesAsync(message.PartitionKey, cancellationToken);
    }

    private async Task ProcessMessageAsync(CdcMessageDto message, CancellationToken cancellationToken)
    {
        // Get routing configuration
        var routingConfig = await _routingService.GetRoutingConfigurationAsync(
            message.TableName,
            cancellationToken);

        if (routingConfig == null)
        {
            throw new RoutingConfigurationNotFoundException(message.TableName);
        }

        // Create CDC event record
        var cdcEvent = new CdcEvent
        {
            Id = Guid.NewGuid(),
            MessageId = message.MessageId,
            TableName = message.TableName,
            Operation = message.Operation,
            Payload = message.Payload,
            SequenceNumber = message.SequenceNumber,
            PartitionKey = message.PartitionKey,
            Timestamp = message.Timestamp,
            CreatedAt = DateTime.UtcNow,
            Status = ProcessingStatus.Processing.ToString(),
            RetryCount = 0
        };

        await _eventRepository.AddAsync(cdcEvent, cancellationToken);

        try
        {
            // Forward message to configured destination
            var forwardMessage = new ForwardMessageDto(
                message.MessageId,
                message.TableName,
                message.Operation,
                message.Payload,
                message.Timestamp
            );

            var published = await _messageBroker.PublishWithConfirmationAsync(
                routingConfig.Exchange,
                routingConfig.RoutingKey,
                forwardMessage,
                cancellationToken);

            if (!published)
            {
                throw new MessageProcessingException(
                    message.MessageId,
                    "Failed to publish message to broker");
            }

            cdcEvent.Status = ProcessingStatus.Completed.ToString();
            cdcEvent.ProcessedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Successfully processed and forwarded message {MessageId} from table {TableName}",
                message.MessageId, message.TableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message {MessageId} from table {TableName}",
                message.MessageId, message.TableName);

            cdcEvent.Status = ProcessingStatus.Failed.ToString();
            cdcEvent.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            await _eventRepository.UpdateAsync(cdcEvent, cancellationToken);
        }
    }

    private async Task ProcessBufferedMessagesAsync(string partitionKey, CancellationToken cancellationToken)
    {
        var bufferedMessages = await _sequenceManager.GetBufferedMessagesAsync(partitionKey, cancellationToken);

        foreach (var bufferedMessage in bufferedMessages.OrderBy(m => m.SequenceNumber))
        {
            var expectedSequence = await _sequenceManager.GetExpectedSequenceAsync(partitionKey, cancellationToken);

            if (bufferedMessage.SequenceNumber == expectedSequence)
            {
                _logger.LogInformation(
                    "Processing buffered message {MessageId} with sequence {Sequence}",
                    bufferedMessage.MessageId, bufferedMessage.SequenceNumber);

                await ProcessMessageAsync(bufferedMessage, cancellationToken);
                await _sequenceManager.UpdateSequenceAsync(partitionKey, bufferedMessage.SequenceNumber, cancellationToken);
                await _idempotencyService.MarkAsProcessedAsync(bufferedMessage.MessageId, TimeSpan.FromDays(7), cancellationToken);
            }
            else
            {
                break; // Stop if there's still a gap
            }
        }
    }
}
