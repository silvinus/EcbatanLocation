using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetDailyOccupation;

public record GetDailyOccupationQuery(DateOnly Date) : IRequest<DailyOccupationDto>;
