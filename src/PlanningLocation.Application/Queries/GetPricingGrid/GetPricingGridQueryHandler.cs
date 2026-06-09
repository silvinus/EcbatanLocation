using MediatR;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Queries.GetPricingGrid;

public class GetPricingGridQueryHandler(
    IPricingGridRepository pricingGridRepository) : IRequestHandler<GetPricingGridQuery, PricingGridDto?>
{
    public async Task<PricingGridDto?> Handle(GetPricingGridQuery request, CancellationToken cancellationToken)
    {
        var grid = await pricingGridRepository.GetByYearAsync(request.Year, cancellationToken);
        if (grid is null) return null;

        var lines = grid.Lines
            .Select(l => new PricingLineDto(l.ClientType, l.PricePerDayPerPerson))
            .ToList();

        return new PricingGridDto(grid.Year, lines);
    }
}
