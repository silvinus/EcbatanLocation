using MediatR;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Commands.UpdateReservation;

public record UpdateReservationCommand(
    Guid ReservationId,
    Guid StudioId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TenantName,
    int AdultCount,
    int ChildrenUnder3Count,
    ClientType ClientType) : IRequest;
