namespace EcbatanLocation.Application.DTOs;

public record PricingGridDto(int Year, IReadOnlyList<PricingLineDto> Lines);
