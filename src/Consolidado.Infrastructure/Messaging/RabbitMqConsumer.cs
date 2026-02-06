using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;
using System.Text;
using System.Text.Json;

namespace Consolidado.Infrastructure.Messaging;

public sealed class RabbitMqConsumer : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName = "lancamentos-criados";
    private const string RetryQueueName = "lancamentos-criados.retry";
    private const string DlqQueueName = "lancamentos-criados.dlq";

    private const string ExchangeName = "lancamentos.exchange";

    private const int MaxAttemptsBeforeDlq = 5;
    private const int RetryDelayMs = 5_000;         

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public RabbitMqConsumer(ConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task StartConsumingAsync(
        Func<LancamentoEfetuadoEvent, Task> handler,
        CancellationToken stoppingToken = default)
    {
        _connection ??= await _factory.CreateConnectionAsync();

        if (_channel is not null && !_channel.IsOpen)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }
        
        _channel ??= await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: DlqQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var retryArgs = new Dictionary<string, object>
        {
            ["x-message-ttl"] = RetryDelayMs,
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = QueueName
        };

        await _channel.QueueDeclareAsync(
            queue: RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArgs,
            cancellationToken: stoppingToken);

        var mainQueueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = RetryQueueName
        };

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainQueueArgs,
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

                LancamentoEfetuadoEvent? evento;
                try
                {
                    evento = JsonSerializer.Deserialize<LancamentoEfetuadoEvent>(json, JsonOptions);
                }
                catch (JsonException)
                {
                    await PublishToDlqAsync(ea, json, reason: "Invalid JSON", ackToken);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ackToken);
                    return;
                }

                if (evento is null)
                {
                    await PublishToDlqAsync(ea, json, reason: "Null event after deserialize", ackToken);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ackToken);
                    return;
                }

                await handler(evento);

                await _channel!.BasicAckAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: ackToken);
            }
            catch (Exception ex)
            {
                var attemptsSoFar = GetTotalDeathCount(ea.BasicProperties);

                if (attemptsSoFar >= MaxAttemptsBeforeDlq)
                {
                    var bodyText = Encoding.UTF8.GetString(ea.Body.ToArray());
                    await PublishToDlqAsync(ea, bodyText, reason: $"Max attempts reached. LastError: {ex.GetType().Name}", ackToken);

                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ackToken);
                    return;
                }

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

    private async Task PublishToDlqAsync(
    BasicDeliverEventArgs ea,
    string bodyText,
    string reason,
    CancellationToken cancellationToken)
    {
        IDictionary<string, object?> headers =
            ea.BasicProperties?.Headers is null
                ? []
                : new Dictionary<string, object?>(ea.BasicProperties.Headers);

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json",
            Headers = headers
        };

        props.Headers["dlq-reason"] = reason;
        props.Headers["dlq-original-exchange"] = ea.Exchange ?? string.Empty;
        props.Headers["dlq-original-routingKey"] = ea.RoutingKey ?? string.Empty;

        var body = Encoding.UTF8.GetBytes(bodyText);

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: DlqQueueName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static int GetTotalDeathCount(IReadOnlyBasicProperties? props)
    {
        try
        {
            if (props?.Headers is null) return 0;
            if (!props.Headers.TryGetValue("x-death", out var xDeathObj)) return 0;

            if (xDeathObj is not IList<object> deaths) return 0;

            var total = 0;

            foreach (var d in deaths)
            {
                if (d is not IDictionary<string, object> dict) continue;

                if (!dict.TryGetValue("count", out var countObj)) continue;

                total += countObj switch
                {
                    byte b => b,
                    short s => s,
                    int i => i,
                    long l => (int)Math.Min(int.MaxValue, l),
                    _ => 0
                };
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
