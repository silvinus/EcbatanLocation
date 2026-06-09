using MediatR;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetReservationDetail;

public record GetReservationDetailQuery(Guid ReservationId) : IRequest<ReservationDetailDto?>;
