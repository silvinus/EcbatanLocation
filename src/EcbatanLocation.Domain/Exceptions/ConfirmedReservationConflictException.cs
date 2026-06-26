namespace EcbatanLocation.Domain.Exceptions;

/// <summary>
/// Thrown when a hypothetical reservation would be staked over a confirmed reservation.
/// A hypothetical bets that a not-yet-confirmed booking (Pending / Accepted) falls through,
/// so it can only be placed over such bookings — never over a confirmed one, whose slot is locked.
/// Inherits from <see cref="InvalidOperationException"/> so existing handlers keep working.
/// </summary>
public sealed class ConfirmedReservationConflictException()
    : InvalidOperationException("A hypothetical reservation cannot be staked over a confirmed reservation.");
