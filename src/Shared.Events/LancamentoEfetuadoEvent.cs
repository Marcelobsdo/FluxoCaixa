namespace Shared.Events;

public record LancamentoEfetuadoEvent(
    Guid EventId,
    Guid LancamentoId,
    Guid ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateTime Data
);


public enum TipoLancamento
{
    Credito = 1,
    Debito = 2
}
