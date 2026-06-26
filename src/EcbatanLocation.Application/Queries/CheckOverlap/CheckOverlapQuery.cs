using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Queries.CheckOverlap;

public record CheckOverlapQuery(
    Guid StudioId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ExcludeReservationId = null) : IRequest<OverlapCheckResult>;
