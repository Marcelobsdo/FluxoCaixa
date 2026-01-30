using Consolidado.Domain.Exceptions;
using Shared.Events;

namespace Consolidado.Domain.Entities;

public sealed class Lancamento
{
    public Guid EventId { get; init; }
    public Guid LancamentoId { get; init; }
    public Guid ComercianteId { get; init; }
    public DateTime Dia { get; init; }
    public TipoLancamento Tipo { get; init; }
    public decimal Valor { get; init; }
    public DateTime ProcessadoEmUtc { get; init; }
}
