using Consolidado.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistence;

public sealed class UnitOfWork(ConsolidadoDbContext db) : IUnitOfWork
{
    private readonly ConsolidadoDbContext _db = db;

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            await action(cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}
