using MediatR;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetStudios;

public record GetStudiosQuery : IRequest<IReadOnlyList<StudioDto>>;
