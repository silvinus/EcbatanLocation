namespace EcbatanLocation.Application.DTOs;

public record ReportPersonLineDto(
    string ClientTypeLabel,
    int AdultCount,
    int ChildrenUnder3Count,
    decimal RatePerDay,
    decimal LineAmount);
