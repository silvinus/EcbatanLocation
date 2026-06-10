using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Behaviors;

namespace EcbatanLocation.Application.Commands.ConfirmReservation;

public record ConfirmReservationCommand(Guid ReservationId, string ConfirmedBy) : IRequest, IRequireAuthorization;
