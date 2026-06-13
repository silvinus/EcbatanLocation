using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Queries.GetReservationReport;

public record GetReservationReportQuery(int Year, int? Month) : IRequest<ReservationReportDto>, IRequireAdmin;
