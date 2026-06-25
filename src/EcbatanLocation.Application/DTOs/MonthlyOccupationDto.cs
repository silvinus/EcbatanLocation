namespace EcbatanLocation.Application.DTOs;

public record MonthlyOccupationDto(
    int Year,
    int Month,
    IReadOnlyList<DailyOccupationDto> Days);
