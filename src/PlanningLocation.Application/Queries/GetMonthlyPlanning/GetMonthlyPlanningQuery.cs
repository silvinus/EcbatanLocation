using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Queries.GetMonthlyPlanning;

public record GetMonthlyPlanningQuery(
    int Year,
    int Month,
    Guid? StudioId = null,
    ReservationStatus? Status = null,
    Guid? OwnerId = null) : IRequest<MonthlyPlanningDto>;
