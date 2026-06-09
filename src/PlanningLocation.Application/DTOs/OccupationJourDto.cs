namespace PlanningLocation.Application.DTOs;

public record OccupationJourDto(
    DateOnly Date,
    int TotalCapacity,
    int OccupiedPlaces,
    int AvailablePlaces,
    int OccupiedStudios,
    int TotalStudios);
