using MediatR;

namespace PlanningLocation.Application.Queries.CheckOverlap;

public record CheckOverlapQuery(
    Guid StudioId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ExcludeReservationId = null) : IRequest<bool>;
