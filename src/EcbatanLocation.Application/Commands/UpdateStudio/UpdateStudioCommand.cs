using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Commands.UpdateStudio;

public record UpdateStudioCommand(
    Guid StudioId,
    string Name,
    int Capacity,
    bool HasKitchen,
    bool RentableAlone,
    bool Unavailable) : IRequest, IRequireAdmin;
