namespace CDC.Application.DTOs;



public class CDCObject
{
    public Schema schema { get; set; }
    public Payload payload { get; set; }
}

public class Schema
{
    public string type { get; set; }
    public Field[] fields { get; set; }
    public bool optional { get; set; }
    public string name { get; set; }
}

public class Field
{
    public string type { get; set; }
    public bool optional { get; set; }
    public int _default { get; set; }
    public string field { get; set; }
    public string name { get; set; }
    public int version { get; set; }
    public Parameters parameters { get; set; }
}

public class Parameters
{
    public string scale { get; set; }
    public string connectdecimalprecision { get; set; }
}

public class Payload
{
    public int id { get; set; }
    public string name { get; set; }
    public string price { get; set; }
    public int stock_quantity { get; set; }
    public long created_at { get; set; }
    public long updated_at { get; set; }
}

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
