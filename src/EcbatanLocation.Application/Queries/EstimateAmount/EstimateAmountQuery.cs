using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.EstimateAmount;

public record EstimateAmountQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<PersonLineDto> PersonLines) : IRequest<decimal>;
