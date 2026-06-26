using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Behaviors;

namespace EcbatanLocation.Application.Commands.DeleteReservation;

public record DeleteReservationCommand(Guid ReservationId)
    : IRequest, IRequireAuthorization, IRequireReservationOwnership;
