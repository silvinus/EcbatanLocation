using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Queries.EstimateAmount;

public class EstimateAmountQueryHandler(
    IPricingGridRepository pricingGridRepository) : IRequestHandler<EstimateAmountQuery, decimal>
{
    public async Task<decimal> Handle(EstimateAmountQuery request, CancellationToken cancellationToken)
    {
        var dates = new DateRange(request.StartDate, request.EndDate);
        var grid = await pricingGridRepository.GetByYearAsync(request.StartDate.Year, cancellationToken)
                   ?? throw new InvalidOperationException($"No pricing grid found for year {request.StartDate.Year}.");

        var personLines = request.PersonLines
            .Select(pl => new PersonLine(pl.ClientType, pl.AdultCount, pl.ChildrenUnder3Count));

        return grid.CalculateAmount(personLines, dates.NumberOfDays);
    }
}
