namespace EcbatanLocation.Application.DTOs;

public record StudioDto(
    Guid Id,
    string Name,
    int Capacity,
    bool HasKitchen,
    bool RentableAlone,
    int DisplayOrder);
