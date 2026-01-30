using Consolidado.Application.Interfaces;
using Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistence;

public sealed class IdempotencyStore(ConsolidadoDbContext db) : IIdempotencyStore
{
    private readonly ConsolidadoDbContext _db = db;

    public async Task<bool> TryMarkProcessedAsync(Guid eventId, string eventName, CancellationToken cancellationToken)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO processed_event (event_id, event_name, processed_at_utc)
            VALUES ({eventId}, {eventName}, NOW())
            ON CONFLICT (event_id) DO NOTHING;
        ", cancellationToken);

        return rows == 1;
    }
}
