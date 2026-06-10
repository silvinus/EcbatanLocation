namespace PlanningLocation.Application.DTOs;

public record RangeOccupationDto(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalCapacity,
    int TotalStudios,
    double AverageOccupiedPlaces,
    double AverageOccupancyRate,
    int DaysInRange);
