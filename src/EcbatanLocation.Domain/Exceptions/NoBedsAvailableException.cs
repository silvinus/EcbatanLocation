namespace EcbatanLocation.Domain.Exceptions;

/// <summary>
/// Thrown when a per-bed studio cannot accommodate a reservation because the requested
/// number of beds (or people) would exceed what remains free over the requested dates.
/// </summary>
public class NoBedsAvailableException : Exception
{
    public NoBedsAvailableException()
        : base("Not enough beds available on this studio for the requested dates.")
    {
    }

    public NoBedsAvailableException(string message) : base(message)
    {
    }
}
