using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record StudioDto(
    Guid Id,
    string Name,
    int Capacity,
    bool HasKitchen,
    bool RentableAlone,
    bool Unavailable,
    int DisplayOrder,
    bool HasReservations = false,
    RentalMode RentalMode = RentalMode.PerLodging,
    int NumberOfBeds = 0);
