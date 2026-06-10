using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetDailyOccupation;

public record GetDailyOccupationQuery(DateOnly Date) : IRequest<DailyOccupationDto>;
