using MediatR;

namespace PlanningLocation.Application.Commands.DeleteReservation;

public record DeleteReservationCommand(Guid ReservationId) : IRequest;
