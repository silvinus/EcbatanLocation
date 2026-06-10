using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetReservationDetail;

public record GetReservationDetailQuery(Guid ReservationId) : IRequest<ReservationDetailDto?>;
