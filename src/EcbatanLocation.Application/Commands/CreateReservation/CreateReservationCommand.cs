using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Commands.CreateReservation;

public record CreateReservationCommand(
    Guid StudioId,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TenantName,
    IReadOnlyList<PersonLineDto> PersonLines,
    Guid? ParentReservationId = null,
    int BedCount = 1,
    bool IsHypothetical = false) : IRequest<Guid>, IRequireAuthorization;
