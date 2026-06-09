using MediatR;

namespace PlanningLocation.Application.Commands.AcceptReservation;

public record AcceptReservationCommand(Guid ReservationId, string AcceptedBy) : IRequest;
