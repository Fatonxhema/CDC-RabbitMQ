namespace CDC.Domain.Enums;

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    RetryScheduled,
    DeadLettered
}

public enum OperationType
{
    Insert,
    Update,
    Delete
}

public enum DlqType
{
    ClientError,    // 4xx errors
    ServerError,    // 5xx errors
    ValidationError,
    MaxRetries
}
