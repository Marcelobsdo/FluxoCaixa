using Lancamentos.Application.Interfaces;
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
            Uri = new Uri(connectionString)
        };
    }

    // Garante inicialização lazy (evita async no construtor)
    private async Task EnsureInitializedAsync(CancellationToken ct)
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
            cancellationToken: ct);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class
    {
        await EnsureInitializedAsync(ct);

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        // Propriedades são opcionais. Se quiser persistência, marque delivery mode (varia conforme API da versão).
        await _channel!.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: string.Empty,
            body: body,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
