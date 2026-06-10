namespace EcbatanLocation.Domain.Events;

public sealed record ReservationCreated(Guid ReservationId, Guid StudioId, Guid OwnerId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record ReservationAccepted(Guid ReservationId, string AcceptedBy) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record ReservationConfirmed(Guid ReservationId, string ConfirmedBy) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record ReservationDeleted(Guid ReservationId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
