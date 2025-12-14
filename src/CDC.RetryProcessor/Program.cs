using CDC.Application.Interfaces;
using CDC.Infrastructure.Caching;
using CDC.Infrastructure.Configuration;
using CDC.Infrastructure.Messaging;
using CDC.Infrastructure.Persistence;
using CDC.RetryProcessor;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Database
builder.Services.AddDbContext<CdcDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")).UseSnakeCaseNamingConvention());

// Redis
var redisConnection = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
    };
    return factory.CreateConnection();
});

// Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IMessageBroker, RabbitMqMessageBroker>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ICdcEventRepository, CdcEventRepository>();
builder.Services.AddScoped<IRoutingConfigurationService, RoutingConfigurationService>();

builder.Services.AddHostedService<RetryWorker>();

var host = builder.Build();
host.Run();
