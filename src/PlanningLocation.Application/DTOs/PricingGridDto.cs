namespace PlanningLocation.Application.DTOs;

public record PricingGridDto(int Year, IReadOnlyList<PricingLineDto> Lines);
