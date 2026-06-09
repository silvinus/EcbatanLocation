using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.DTOs;

public record ReservationPlanningDto(
    Guid Id,
    string TenantName,
    string OwnerName,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    ReservationStatus Status,
    ClientType ClientType,
    int AdultCount,
    int ChildrenUnder3Count);
