using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record ReservationPlanningDto(
    Guid Id,
    string TenantName,
    string OwnerName,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    ReservationStatus Status,
    IReadOnlyList<PersonLineDto> PersonLines);
