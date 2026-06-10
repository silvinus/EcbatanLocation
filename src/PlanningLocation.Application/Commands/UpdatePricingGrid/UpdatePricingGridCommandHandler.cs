using PlanningLocation.Application.Messaging;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Commands.UpdatePricingGrid;

public class UpdatePricingGridCommandHandler(
    IPricingGridRepository pricingGridRepository) : IRequestHandler<UpdatePricingGridCommand>
{
    public async Task Handle(UpdatePricingGridCommand request, CancellationToken cancellationToken)
    {
        var lines = request.Lines
            .Select(l => PricingLine.Create(l.ClientType, l.PricePerDayPerPerson))
            .ToList();

        var existingGrid = await pricingGridRepository.GetByYearAsync(request.Year, cancellationToken);

        if (existingGrid is not null)
        {
            existingGrid.Update(lines);
            await pricingGridRepository.UpdateAsync(existingGrid, cancellationToken);
        }
        else
        {
            var newGrid = PricingGrid.Create(request.Year, lines);
            await pricingGridRepository.AddAsync(newGrid, cancellationToken);
        }
    }
}
