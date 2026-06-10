namespace EcbatanLocation.Application.DTOs;

public record DailyOccupationDto(
    DateOnly Date,
    int TotalCapacity,
    int OccupiedPlaces,
    int AvailablePlaces,
    int OccupiedStudios,
    int TotalStudios);
