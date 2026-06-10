using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.Behaviors;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Commands.CreateReservation;

public record CreateReservationCommand(
    Guid StudioId,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TenantName,
    IReadOnlyList<PersonLineDto> PersonLines) : IRequest<Guid>, IRequireAuthorization;
