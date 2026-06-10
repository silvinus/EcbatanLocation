namespace EcbatanLocation.Domain.Exceptions;

/// <summary>
/// Thrown when a reservation would overlap an existing one on the same studio.
/// Inherits from <see cref="InvalidOperationException"/> so existing handlers keep working.
/// </summary>
public sealed class OverlappingReservationException()
    : InvalidOperationException("A reservation already exists on this studio for the requested dates.");
