using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetMonthlyOccupation;

public record GetMonthlyOccupationQuery(int Year, int Month) : IRequest<MonthlyOccupationDto>;
