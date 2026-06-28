using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.CreateStudio;

public class CreateStudioCommandHandler(
    IStudioRepository studioRepository) : IRequestHandler<CreateStudioCommand>
{
    public async Task Handle(CreateStudioCommand request, CancellationToken cancellationToken)
    {
        var maxOrder = await studioRepository.GetMaxDisplayOrderAsync(cancellationToken);

        var studio = Studio.Create(
            request.Name,
            request.Capacity,
            request.HasKitchen,
            request.RentableAlone,
            maxOrder + 1,
            request.Unavailable,
            request.RentalMode,
            request.NumberOfBeds);

        await studioRepository.AddAsync(studio, cancellationToken);
    }
}
