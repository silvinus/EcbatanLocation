using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetOwnerByUserId;

public record GetOwnerByUserIdQuery(string UserId) : IRequest<OwnerDto?>;
