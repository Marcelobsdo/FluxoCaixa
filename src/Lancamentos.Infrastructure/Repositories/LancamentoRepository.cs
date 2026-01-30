using Lancamentos.Domain.Interfaces;
using Lancamentos.Domain.Entities;
using Lancamentos.Infrastructure.Persistence;

namespace Lancamentos.Infrastructure.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly LancamentosDbContext _context;

    public LancamentoRepository(LancamentosDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Lancamento lancamento, CancellationToken ct)
    {
        await _context.Lancamentos.AddAsync(lancamento, ct);
        await _context.SaveChangesAsync(ct);
    }
}
