namespace Consolidado.Application.Interfaces;

public interface IIdempotencyStore
{
    Task<bool> TryMarkProcessedAsync(Guid eventId, string eventName, CancellationToken cancellationToken);
}
