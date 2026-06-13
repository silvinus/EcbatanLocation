namespace EcbatanLocation.Application.DTOs;

public record ReservationReportDto(
    int Year,
    int? Month,
    string PeriodLabel,
    IReadOnlyList<ReportLineDto> Lines,
    ReportSummaryDto Summary,
    DateTime GeneratedAt);
