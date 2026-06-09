using MediatR;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetRangeOccupation;

public record GetRangeOccupationQuery(DateOnly StartDate, DateOnly EndDate) : IRequest<OccupationRangeDto>;
