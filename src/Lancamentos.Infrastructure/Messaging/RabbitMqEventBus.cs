using Lancamentos.Application.Interfaces;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Lancamentos.Infrastructure.Messaging;

public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private const string ExchangeName = "lancamentos.exchange";

    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventBus(string connectionString)
    {
        _factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
    }

    private static readonly AsyncRetryPolicy RetryPolicy =
    Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 4,
            sleepDurationProvider: attempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 200)
        );

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _channel is not null)
            return;

        _connection ??= await _factory.CreateConnectionAsync();

        _channel ??= await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await RetryPolicy.ExecuteAsync(async () =>
        {
            await EnsureInitializedAsync(cancellationToken);

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel!.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: string.Empty,
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
