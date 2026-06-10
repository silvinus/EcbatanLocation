using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetReservationDetail;

public record GetReservationDetailQuery(Guid ReservationId) : IRequest<ReservationDetailDto?>;
