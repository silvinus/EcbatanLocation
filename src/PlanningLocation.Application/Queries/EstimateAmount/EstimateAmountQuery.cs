using MediatR;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Queries.EstimateAmount;

public record EstimateAmountQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    int AdultCount,
    int ChildrenUnder3Count,
    ClientType ClientType) : IRequest<decimal>;
