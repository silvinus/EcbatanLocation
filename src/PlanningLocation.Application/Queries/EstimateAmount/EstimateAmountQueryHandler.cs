using MediatR;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Application.Queries.EstimateAmount;

public class EstimateAmountQueryHandler(
    IPricingGridRepository pricingGridRepository) : IRequestHandler<EstimateAmountQuery, decimal>
{
    public async Task<decimal> Handle(EstimateAmountQuery request, CancellationToken cancellationToken)
    {
        var dates = new DateRange(request.StartDate, request.EndDate);
        var grid = await pricingGridRepository.GetByYearAsync(request.StartDate.Year, cancellationToken)
                   ?? throw new InvalidOperationException($"No pricing grid found for year {request.StartDate.Year}.");

        var total = 0m;
        foreach (var line in request.PersonLines)
        {
            var rate = grid.GetRate(line.ClientType);
            var childRate = line.ClientType == ClientType.Acquaintance ? rate * 0.5m : rate;
            total += (line.AdultCount * rate + line.ChildrenUnder3Count * childRate) * dates.NumberOfDays;
        }

        return total;
    }
}
