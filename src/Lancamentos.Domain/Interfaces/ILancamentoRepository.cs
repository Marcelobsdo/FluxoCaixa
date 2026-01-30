using Lancamentos.Domain.Entities;

namespace Lancamentos.Domain.Interfaces;

public interface ILancamentoRepository
{
    Task AddAsync(Lancamento lancamento, CancellationToken ct);
}