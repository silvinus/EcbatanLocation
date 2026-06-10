using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.UpdatePricingGrid;

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
