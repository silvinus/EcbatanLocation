using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.UpdateStudio;

public class UpdateStudioCommandHandler(
    IStudioRepository studioRepository) : IRequestHandler<UpdateStudioCommand>
{
    public async Task Handle(UpdateStudioCommand request, CancellationToken cancellationToken)
    {
        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        studio.Update(request.Name, request.Capacity, request.HasKitchen, request.RentableAlone, request.Unavailable);

        await studioRepository.UpdateAsync(studio, cancellationToken);
    }
}
