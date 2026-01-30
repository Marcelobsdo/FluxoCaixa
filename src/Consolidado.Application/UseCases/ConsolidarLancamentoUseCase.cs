using Consolidado.Application.Interfaces;
using Consolidado.Domain.Entities;
using Consolidado.Domain.Interfaces;
using Shared.Events;

namespace Consolidado.Application.UseCases;

public sealed class ConsolidarLancamentoUseCase
{
    private readonly ILancamentoRepository _lancamentos;
    private readonly IIdempotencyStore _idempotency;
    private readonly IUnitOfWork _unitOfWork;

    public ConsolidarLancamentoUseCase(
        ILancamentoRepository lancamentos,
        IIdempotencyStore idempotency,
        IUnitOfWork unitOfWork)
    {
        _lancamentos = lancamentos;
        _idempotency = idempotency;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(LancamentoEfetuadoEvent evento, CancellationToken ct)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var firstTime = await _idempotency.TryMarkProcessedAsync(
                evento.EventId, nameof(LancamentoEfetuadoEvent), innerCt);

            if (!firstTime) return;

            var lancamento = new Lancamento
            {
                EventId = evento.EventId,
                LancamentoId = evento.LancamentoId,
                ComercianteId = evento.ComercianteId,
                Dia = evento.Data.Date,
                Tipo = evento.Tipo,
                Valor = evento.Valor,
                ProcessadoEmUtc = DateTime.UtcNow
            };

            await _lancamentos.AddAsync(lancamento, innerCt);
        }, ct);
    }
}

