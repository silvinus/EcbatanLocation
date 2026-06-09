using MediatR;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetOwnerByUserId;

public record GetOwnerByUserIdQuery(string UserId) : IRequest<OwnerDto?>;
