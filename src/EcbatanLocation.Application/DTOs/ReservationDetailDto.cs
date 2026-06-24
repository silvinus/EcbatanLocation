using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record ReservationDetailDto(
    Guid Id,
    StudioDto Studio,
    OwnerDto Owner,
    DateOnly StartDate,
    DateOnly EndDate,
    int NumberOfDays,
    string TenantName,
    IReadOnlyList<PersonLineDto> PersonLines,
    ReservationStatus Status,
    string? AcceptedBy,
    DateTime? AcceptedAt,
    string? ConfirmedBy,
    DateTime? ConfirmedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    decimal? EstimatedAmount,
    Guid? ParentReservationId = null,
    string? ParentStudioName = null,
    string? ParentTenantName = null,
    IReadOnlyList<DependentReservationSummaryDto>? Dependents = null)
{
    public bool IsDependent => ParentReservationId.HasValue;
}
