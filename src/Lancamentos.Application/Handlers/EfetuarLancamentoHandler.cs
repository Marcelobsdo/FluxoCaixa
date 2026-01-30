using Lancamentos.Application.Commands;
using Lancamentos.Application.Interfaces;
using Lancamentos.Domain.Entities;
using Lancamentos.Domain.Interfaces;
using MediatR;
using Shared.Events;

namespace Lancamentos.Application.Handlers;

public class EfetuarLancamentoHandler
    : IRequestHandler<EfetuarLancamentoCommand, Guid>
{
    private readonly ILancamentoRepository _repository;
    private readonly IEventBus _eventBus;

    public EfetuarLancamentoHandler(
        ILancamentoRepository repository,
        IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(
        EfetuarLancamentoCommand command,
        CancellationToken cancellationToken)
    {
        var lancamento = Lancamento.Criar(
            command.ComercianteId,
            command.Valor,
            command.Tipo,
            command.Data);

        await _repository.AddAsync(lancamento, cancellationToken);

        var evento = new LancamentoEfetuadoEvent(
            Guid.NewGuid(),
            lancamento.Id,
            command.ComercianteId,
            lancamento.Valor,
            Map(lancamento.Tipo),
            lancamento.Data);

        await _eventBus.PublishAsync(evento, cancellationToken);

        return lancamento.Id;
    }

    private static TipoLancamento Map(
        Domain.Enums.TipoLancamento tipo) => tipo switch
        {
            Domain.Enums.TipoLancamento.Credito => TipoLancamento.Credito,
            Domain.Enums.TipoLancamento.Debito => TipoLancamento.Debito,
            _ => throw new ArgumentOutOfRangeException()
        };
}
