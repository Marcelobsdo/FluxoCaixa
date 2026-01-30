using Lancamentos.Domain.Enums;
using MediatR;

namespace Lancamentos.Application.Commands;

public record EfetuarLancamentoCommand(
    Guid ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateTime Data
) : IRequest<Guid>;