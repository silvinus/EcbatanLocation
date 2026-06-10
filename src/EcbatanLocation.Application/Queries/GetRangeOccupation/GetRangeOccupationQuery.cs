using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetRangeOccupation;

public record GetRangeOccupationQuery(DateOnly StartDate, DateOnly EndDate) : IRequest<RangeOccupationDto>;
