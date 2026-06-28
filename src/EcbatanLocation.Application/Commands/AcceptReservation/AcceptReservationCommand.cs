using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Behaviors;

namespace EcbatanLocation.Application.Commands.AcceptReservation;

public record AcceptReservationCommand(Guid ReservationId, string AcceptedBy)
    : IRequest, IRequireAuthorization, IRequireReservationOwnership;
