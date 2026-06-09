using MediatR;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Commands.CreateReservation;

public record CreateReservationCommand(
    Guid StudioId,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TenantName,
    int AdultCount,
    int ChildrenUnder3Count,
    ClientType ClientType) : IRequest<Guid>;
