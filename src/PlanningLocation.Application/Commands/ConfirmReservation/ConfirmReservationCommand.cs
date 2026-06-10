using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.Behaviors;

namespace PlanningLocation.Application.Commands.ConfirmReservation;

public record ConfirmReservationCommand(Guid ReservationId, string ConfirmedBy) : IRequest, IRequireAuthorization;
