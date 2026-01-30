namespace Consolidado.Application.Interfaces;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
