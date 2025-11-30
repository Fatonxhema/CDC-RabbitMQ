namespace CDC.Domain.Entities;

public class CdcEvent
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public string PartitionKey { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
