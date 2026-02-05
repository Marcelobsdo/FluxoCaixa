using Consolidado.Application.UseCases;
using Consolidado.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Consolidado.Worker;

public sealed class Worker(
    RabbitMqConsumer consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly RabbitMqConsumer _consumer = consumer;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consolidado.Worker iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _consumer.StartConsumingAsync(async evento =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var useCase = scope.ServiceProvider.GetRequiredService<ConsolidarLancamentoUseCase>();

                        await useCase.ExecuteAsync(evento, stoppingToken);

                        _logger.LogInformation(
                            "Evento {EventId} (Lancamento {LancamentoId}) consolidado com sucesso",
                            evento.EventId, evento.LancamentoId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Erro ao processar evento {EventId} (Lancamento {LancamentoId})",
                            evento.EventId, evento.LancamentoId);
                    }
                }, stoppingToken);

                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ indisponível no startup. Tentando novamente em 5s...");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
