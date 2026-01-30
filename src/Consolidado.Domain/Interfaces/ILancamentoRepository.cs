using Consolidado.Domain.Entities;

namespace Consolidado.Domain.Interfaces;

public interface ILancamentoRepository
{
    Task AddAsync(Lancamento saldo, CancellationToken ct);
}
