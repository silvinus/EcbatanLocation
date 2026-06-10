using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetOwners;

public record GetOwnersQuery : IRequest<IReadOnlyList<OwnerDto>>;
