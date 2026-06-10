using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.Behaviors;

namespace PlanningLocation.Application.Commands.DeleteReservation;

public record DeleteReservationCommand(Guid ReservationId) : IRequest, IRequireAuthorization;
