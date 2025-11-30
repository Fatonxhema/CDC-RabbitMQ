namespace CDC.Application.DTOs;

public record CdcMessageDto(
    string MessageId,
    string TableName,
    string Operation,
    string Payload,
    long SequenceNumber,
    string PartitionKey,
    DateTime Timestamp
);

public record ForwardMessageDto(
    string MessageId,
    string TableName,
    string Operation,
    string Payload,
    DateTime Timestamp
);

public record ProcessingResultDto(
    bool Success,
    int? StatusCode,
    string? ErrorMessage
);

public record RetryMessageDto(
    string MessageId,
    string TableName,
    string Payload,
    int RetryCount,
    DateTime OriginalTimestamp,
    string? LastError
);
