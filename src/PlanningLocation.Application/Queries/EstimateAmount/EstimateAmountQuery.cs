using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.EstimateAmount;

public record EstimateAmountQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<PersonLineDto> PersonLines) : IRequest<decimal>;
