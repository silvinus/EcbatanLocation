using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Queries.GetCompatibleParents;

public record GetCompatibleParentsQuery(
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ExcludeReservationId = null) : IRequest<IReadOnlyList<ParentReservationOptionDto>>, IRequireAuthorization;
