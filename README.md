# CDC Pipeline with RabbitMQ, Debezium, and .NET 8

A production-ready Change Data Capture (CDC) pipeline built with clean architecture principles, featuring:
- PostgreSQL CDC using Debezium
- RabbitMQ for message brokering
- Redis for caching and sequence management
- Ordering guarantees for message processing
- Multiple DLQ strategies
- Comprehensive retry mechanisms
- Idempotency support

## Architecture

```
PostgreSQL (CDC) → Debezium → RabbitMQ → Consumer API → Listener API
                                             ↓
                                    Retry Processor
                                             ↓
                                    Multiple DLQs
```

## Projects

- **CDC.Domain**: Core domain entities, enums, and exceptions
- **CDC.Application**: Application services and interfaces
- **CDC.Infrastructure**: Infrastructure implementations (RabbitMQ, Redis, EF Core)
- **CDC.Consumer.API**: Consumes CDC events and forwards to configured destinations
- **CDC.Listener.API**: Receives and processes forwarded events
- **CDC.RetryProcessor**: Background worker for retry logic

## Features

### 1. Clean Architecture
- Separation of concerns
- Dependency inversion
- Testable code
- SOLID principles

### 2. Ordering Guarantees
- Sequence number validation
- Message buffering for out-of-order events
- Partition-based ordering using Redis

### 3. Idempotency
- Message deduplication
- Redis-based processed message tracking
- Prevents duplicate processing

### 4. Multiple DLQ Strategies
- Client errors (4xx) → Separate DLQ
- Server errors (5xx) → Separate DLQ
- Validation errors → Dedicated DLQ
- Max retries exceeded → Dead letter queue

### 5. Retry Mechanism
- Exponential backoff
- Configurable retry limits
- Polly for resilience
- Automatic retry scheduling

## Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL 15+
- RabbitMQ 3.12+
- Redis 7+

## Getting Started

### 1. Start Infrastructure

```bash
cd infrastructure/docker
docker-compose up -d
```

### 2. Configure Debezium

```bash
curl -X POST http://localhost:8083/connectors \
  -H "Content-Type: application/json" \
  -d @debezium-connector-config.json
```

### 3. Run Database Migrations

```bash
cd src/CDC.Consumer.API
dotnet ef database update
```

### 4. Start Applications

```bash
# Terminal 1 - Consumer API
cd src/CDC.Consumer.API
dotnet run

# Terminal 2 - Listener API
cd src/CDC.Listener.API
dotnet run

# Terminal 3 - Retry Processor
cd src/CDC.RetryProcessor
dotnet run
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cdc_pipeline;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672",
    "Username": "guest",
    "Password": "guest"
  }
}
```

## How It Works

### 1. CDC Events Flow
1. Database changes are captured by Debezium
2. Events are published to RabbitMQ `cdc.events` queue
3. Consumer API receives events with sequence numbers
4. Sequence validation ensures ordering
5. Events are forwarded to configured destinations
6. Listener API processes the forwarded events

### 2. Sequence Management
```csharp
// Check if sequence is valid
var isValid = await sequenceManager.ValidateSequenceAsync(partitionKey, sequenceNumber);

// Buffer if out of order
if (!isValid)
{
    await sequenceManager.BufferMessageAsync(partitionKey, sequenceNumber, message);
    return;
}

// Process in order
await ProcessMessageAsync(message);
await sequenceManager.UpdateSequenceAsync(partitionKey, sequenceNumber);
```

### 3. Retry Logic
- Failed messages are marked for retry
- Exponential backoff strategy
- Maximum 5 retry attempts
- After max retries → Dead letter queue

## Monitoring

### Health Checks
- Consumer API: http://localhost:5000/health
- Database connectivity
- Redis connectivity
- RabbitMQ connectivity

### RabbitMQ Management
- URL: http://localhost:15672
- Username: guest
- Password: guest

### Metrics
- Message processing rate
- Retry counts
- DLQ statistics
- Sequence gaps

## Testing

```bash
# Insert test data
psql -h localhost -U postgres -d cdc_pipeline
INSERT INTO customers (name, email) VALUES ('John Doe', 'john@example.com');
```

## Production Considerations

1. **Scaling**: Use RabbitMQ consistent hash exchange for partitioning
2. **Monitoring**: Integrate with Prometheus/Grafana
3. **Security**: Use TLS for RabbitMQ and PostgreSQL connections
4. **High Availability**: Run multiple instances of each service
5. **Backup**: Regular database backups with point-in-time recovery

## License

MIT License

## Contributing

Pull requests are welcome!
