namespace PlanningLocation.Application.DTOs;

public record PlanningMensuelDto(
    int Year,
    int Month,
    IReadOnlyList<StudioPlanningDto> Studios);
