using Consolidado.Domain.Entities;
using Consolidado.Domain.Interfaces;

namespace Consolidado.Infrastructure.Persistence;

public sealed class LancamentoRepository(ConsolidadoDbContext db) : ILancamentoRepository
{
    private readonly ConsolidadoDbContext _db = db;

    public async Task AddAsync(Lancamento lancamento, CancellationToken ct)
        => await _db.Lancamentos.AddAsync(lancamento, ct);
}
