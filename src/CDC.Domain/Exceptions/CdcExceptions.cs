namespace CDC.Domain.Exceptions;

public class CdcException : Exception
{
    public CdcException(string message) : base(message) { }
    public CdcException(string message, Exception innerException) : base(message, innerException) { }
}

public class SequenceException : CdcException
{
    public long ExpectedSequence { get; }
    public long ReceivedSequence { get; }

    public SequenceException(long expected, long received)
        : base($"Sequence mismatch. Expected: {expected}, Received: {received}")
    {
        ExpectedSequence = expected;
        ReceivedSequence = received;
    }
}

public class RoutingConfigurationNotFoundException : CdcException
{
    public RoutingConfigurationNotFoundException(string tableName)
        : base($"Routing configuration not found for table: {tableName}") { }
}

public class MessageProcessingException : CdcException
{
    public string MessageId { get; }

    public MessageProcessingException(string messageId, string message)
        : base(message)
    {
        MessageId = messageId;
    }

    public MessageProcessingException(string messageId, string message, Exception innerException)
        : base(message, innerException)
    {
        MessageId = messageId;
    }
}
