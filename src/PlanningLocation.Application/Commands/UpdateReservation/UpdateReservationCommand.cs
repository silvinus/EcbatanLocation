using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.Behaviors;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Commands.UpdateReservation;

public record UpdateReservationCommand(
    Guid ReservationId,
    Guid StudioId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TenantName,
    IReadOnlyList<PersonLineDto> PersonLines) : IRequest, IRequireAuthorization;
