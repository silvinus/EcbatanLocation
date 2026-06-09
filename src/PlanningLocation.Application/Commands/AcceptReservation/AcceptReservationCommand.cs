using MediatR;
using PlanningLocation.Application.Behaviors;

namespace PlanningLocation.Application.Commands.AcceptReservation;

public record AcceptReservationCommand(Guid ReservationId, string AcceptedBy) : IRequest, IRequireAuthorization;
