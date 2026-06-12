namespace EcbatanLocation.Application.DTOs;

public record UserDto(
    string UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsOwner,
    bool HasReservations);
