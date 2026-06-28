using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Commands.UpdateReservation;

public record UpdateReservationCommand(
    Guid ReservationId,
    Guid StudioId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TenantName,
    IReadOnlyList<PersonLineDto> PersonLines,
    Guid? ParentReservationId = null,
    int BedCount = 1,
    bool IsHypothetical = false) : IRequest, IRequireAuthorization, IRequireReservationOwnership;
