using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.Queries.GetMonthlyPlanning;

public record GetMonthlyPlanningQuery(
    int Year,
    int Month,
    Guid? StudioId = null,
    ReservationStatus? Status = null,
    Guid? OwnerId = null) : IRequest<MonthlyPlanningDto>;
