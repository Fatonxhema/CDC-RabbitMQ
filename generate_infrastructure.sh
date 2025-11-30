#!/bin/bash

# This script generates all remaining CDC Pipeline project files

BASE_DIR="/home/claude/CDCPipeline"

# Create RabbitMQ Message Broker implementation
cat > "$BASE_DIR/src/CDC.Infrastructure/Messaging/RabbitMqMessageBroker.cs" << 'EOF'
using CDC.Application.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CDC.Infrastructure.Messaging;

public class RabbitMqMessageBroker : IMessageBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBroker> _logger;

    public RabbitMqMessageBroker(IConnection connection, ILogger<RabbitMqMessageBroker> logger)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _channel.ConfirmSelect();
        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(exchange, routingKey, properties, body);
        
        _logger.LogDebug("Published message to exchange {Exchange} with routing key {RoutingKey}", exchange, routingKey);
        
        await Task.CompletedTask;
    }

    public async Task<bool> PublishWithConfirmationAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(exchange, routingKey, message, cancellationToken);
        
        try
        {
            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm message publication");
            return false;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
EOF

# Create Redis Cache Service
cat > "$BASE_DIR/src/CDC.Infrastructure/Caching/RedisCacheService.cs" << 'EOF'
using CDC.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace CDC.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
        return true;
    }

    public async Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        return await db.StringSetAsync(key, json, expiration, When.NotExists);
    }

    public async Task<long> IncrementAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.StringIncrementAsync(key);
    }

    public async Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.StringSetAsync(key, value, expiration, When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";
        var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { value });
        return (int)result == 1;
    }
}
EOF

# Create Sequence Manager
cat > "$BASE_DIR/src/CDC.Infrastructure/Caching/SequenceManager.cs" << 'EOF'
using CDC.Application.DTOs;
using CDC.Application.Interfaces;
using System.Text.Json;

namespace CDC.Infrastructure.Caching;

public class SequenceManager : ISequenceManager
{
    private readonly ICacheService _cacheService;
    private const string SequencePrefix = "seq:";
    private const string BufferPrefix = "buffer:";

    public SequenceManager(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<bool> ValidateSequenceAsync(string partitionKey, long sequenceNumber, CancellationToken cancellationToken = default)
    {
        var expectedSequence = await GetExpectedSequenceAsync(partitionKey, cancellationToken);
        return sequenceNumber == expectedSequence;
    }

    public async Task UpdateSequenceAsync(string partitionKey, long sequenceNumber, CancellationToken cancellationToken = default)
    {
        var key = $"{SequencePrefix}{partitionKey}";
        await _cacheService.SetAsync(key, sequenceNumber + 1, cancellationToken: cancellationToken);
    }

    public async Task<long> GetExpectedSequenceAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        var key = $"{SequencePrefix}{partitionKey}";
        var sequence = await _cacheService.GetAsync<long?>(key, cancellationToken);
        return sequence ?? 0;
    }

    public async Task BufferMessageAsync(string partitionKey, long sequenceNumber, CdcMessageDto message, CancellationToken cancellationToken = default)
    {
        var key = $"{BufferPrefix}{partitionKey}:{sequenceNumber}";
        await _cacheService.SetAsync(key, message, TimeSpan.FromHours(1), cancellationToken);
    }

    public async Task<List<CdcMessageDto>> GetBufferedMessagesAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        // This is simplified - in production, use Redis SCAN for pattern matching
        var messages = new List<CdcMessageDto>();
        
        for (long i = 0; i < 100; i++) // Check next 100 sequences
        {
            var key = $"{BufferPrefix}{partitionKey}:{i}";
            var message = await _cacheService.GetAsync<CdcMessageDto>(key, cancellationToken);
            if (message != null)
            {
                messages.Add(message);
                await _cacheService.DeleteAsync(key, cancellationToken);
            }
        }

        return messages;
    }
}
EOF

# Create Idempotency Service
cat > "$BASE_DIR/src/CDC.Infrastructure/Caching/IdempotencyService.cs" << 'EOF'
using CDC.Application.Interfaces;

namespace CDC.Infrastructure.Caching;

public class IdempotencyService : IIdempotencyService
{
    private readonly ICacheService _cacheService;
    private const string IdempotencyPrefix = "processed:";

    public IdempotencyService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var key = $"{IdempotencyPrefix}{messageId}";
        var value = await _cacheService.GetAsync<bool?>(key, cancellationToken);
        return value.HasValue && value.Value;
    }

    public async Task MarkAsProcessedAsync(string messageId, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var key = $"{IdempotencyPrefix}{messageId}";
        expiration ??= TimeSpan.FromDays(7);
        await _cacheService.SetAsync(key, true, expiration, cancellationToken);
    }
}
EOF

# Create DbContext
cat > "$BASE_DIR/src/CDC.Infrastructure/Persistence/CdcDbContext.cs" << 'EOF'
using CDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CDC.Infrastructure.Persistence;

public class CdcDbContext : DbContext
{
    public CdcDbContext(DbContextOptions<CdcDbContext> options) : base(options) { }

    public DbSet<CdcEvent> CdcEvents { get; set; }
    public DbSet<RoutingConfiguration> RoutingConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CdcEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.PartitionKey, e.SequenceNumber });
            entity.Property(e => e.Payload).HasColumnType("jsonb");
        });

        modelBuilder.Entity<RoutingConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TableName).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });
    }
}
EOF

# Create Repository
cat > "$BASE_DIR/src/CDC.Infrastructure/Persistence/CdcEventRepository.cs" << 'EOF'
using CDC.Application.Interfaces;
using CDC.Domain.Entities;
using CDC.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CDC.Infrastructure.Persistence;

public class CdcEventRepository : ICdcEventRepository
{
    private readonly CdcDbContext _context;

    public CdcEventRepository(CdcDbContext context)
    {
        _context = context;
    }

    public async Task<CdcEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CdcEvents.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<CdcEvent?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.CdcEvents
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(CdcEvent cdcEvent, CancellationToken cancellationToken = default)
    {
        await _context.CdcEvents.AddAsync(cdcEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CdcEvent cdcEvent, CancellationToken cancellationToken = default)
    {
        _context.CdcEvents.Update(cdcEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CdcEvent>> GetPendingRetries(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.CdcEvents
            .Where(e => e.Status == ProcessingStatus.RetryScheduled.ToString() || e.Status == ProcessingStatus.Failed.ToString())
            .Where(e => e.RetryCount < 5)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
EOF

# Create Routing Configuration Service
cat > "$BASE_DIR/src/CDC.Infrastructure/Configuration/RoutingConfigurationService.cs" << 'EOF'
using CDC.Application.Interfaces;
using CDC.Domain.Entities;
using CDC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CDC.Infrastructure.Configuration;

public class RoutingConfigurationService : IRoutingConfigurationService
{
    private readonly CdcDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "routing:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public RoutingConfigurationService(CdcDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<RoutingConfiguration?> GetRoutingConfigurationAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{tableName}";

        if (_cache.TryGetValue(cacheKey, out RoutingConfiguration? cachedConfig))
        {
            return cachedConfig;
        }

        var config = await _context.RoutingConfigurations
            .FirstOrDefaultAsync(r => r.TableName == tableName && r.IsActive, cancellationToken);

        if (config != null)
        {
            _cache.Set(cacheKey, config, CacheDuration);
        }

        return config;
    }

    public async Task<List<RoutingConfiguration>> GetAllActiveConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RoutingConfigurations
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);
    }
}
EOF

echo "Infrastructure layer files created successfully!"
EOF
chmod +x "$BASE_DIR/generate_infrastructure.sh"
bash "$BASE_DIR/generate_infrastructure.sh"
