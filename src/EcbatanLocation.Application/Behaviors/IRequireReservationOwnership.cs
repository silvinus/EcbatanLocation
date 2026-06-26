namespace EcbatanLocation.Application.Behaviors;

/// <summary>
/// Marks a command that acts on an existing reservation and may be performed only by the
/// reservation's owner or by an administrator. <see cref="ReservationId"/> identifies the target.
/// </summary>
public interface IRequireReservationOwnership
{
    Guid ReservationId { get; }
}
