namespace PlanningLocation.Application.DTOs;

public record MonthlyPlanningDto(
    int Year,
    int Month,
    IReadOnlyList<StudioPlanningDto> Studios);
