using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using System.Text;
using System.Text.Json;

namespace Consolidado.Infrastructure.Messaging;

public sealed class RabbitMqConsumer : IAsyncDisposable
{
    private readonly IConnection _connection;
    private IChannel? _channel;

    private const string QueueName = "lancamentos-criados";
    private const string ExchangeName = "lancamentos.exchange";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public RabbitMqConsumer(IConnection connection)
    {
        _connection = connection;
    }

    public async Task StartConsumingAsync(
        Func<LancamentoEfetuadoEvent, Task> handler,
        CancellationToken stoppingToken = default)
    {
        _channel ??= await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 50,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var ackToken = CancellationToken.None;

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var evento = JsonSerializer.Deserialize<LancamentoEfetuadoEvent>(
                    json,
                    JsonOptions);

                if (evento is null)
                {
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: ackToken);
                    return;
                }

                await handler(evento);

                await _channel.BasicAckAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: ackToken);
            }
            catch (JsonException)
            {
                await _channel!.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: ackToken);
            }
            catch
            {
                await _channel!.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: ackToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
    }
}
