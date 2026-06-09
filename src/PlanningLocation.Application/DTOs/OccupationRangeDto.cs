namespace PlanningLocation.Application.DTOs;

public record OccupationRangeDto(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalCapacity,
    int TotalStudios,
    double AverageOccupiedPlaces,
    double AverageOccupancyRate,
    int DaysInRange);
