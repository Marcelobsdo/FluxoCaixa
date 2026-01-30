namespace Consolidado.Domain.Entities;

public sealed class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public string EventName { get; private set; } = default!;
    public DateTime ProcessedAtUtc { get; private set; }

    protected ProcessedEvent() { }
}
