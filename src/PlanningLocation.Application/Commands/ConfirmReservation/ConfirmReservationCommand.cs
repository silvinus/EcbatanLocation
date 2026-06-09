using MediatR;

namespace PlanningLocation.Application.Commands.ConfirmReservation;

public record ConfirmReservationCommand(Guid ReservationId, string ConfirmedBy) : IRequest;
