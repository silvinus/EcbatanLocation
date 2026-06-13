using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Commands.CreateStudio;

public record CreateStudioCommand(
    string Name,
    int Capacity,
    bool HasKitchen,
    bool RentableAlone,
    bool Unavailable) : IRequest, IRequireAdmin;
