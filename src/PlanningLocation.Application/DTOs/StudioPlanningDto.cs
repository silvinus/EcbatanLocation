namespace PlanningLocation.Application.DTOs;

public record StudioPlanningDto(
    StudioDto Studio,
    IReadOnlyList<ReservationPlanningDto> Reservations);
