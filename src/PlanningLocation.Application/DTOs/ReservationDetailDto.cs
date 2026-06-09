using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.DTOs;

public record ReservationDetailDto(
    Guid Id,
    StudioDto Studio,
    OwnerDto Owner,
    DateOnly StartDate,
    DateOnly EndDate,
    int NumberOfDays,
    string TenantName,
    int AdultCount,
    int ChildrenUnder3Count,
    ClientType ClientType,
    ReservationStatus Status,
    string? AcceptedBy,
    DateTime? AcceptedAt,
    string? ConfirmedBy,
    DateTime? ConfirmedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    decimal? EstimatedAmount);
