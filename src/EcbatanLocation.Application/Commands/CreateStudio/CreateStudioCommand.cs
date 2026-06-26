using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.Commands.CreateStudio;

public record CreateStudioCommand(
    string Name,
    int Capacity,
    bool HasKitchen,
    bool RentableAlone,
    bool Unavailable,
    RentalMode RentalMode = RentalMode.PerLodging,
    int NumberOfBeds = 0) : IRequest, IRequireAdmin;
