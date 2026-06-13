namespace EcbatanLocation.Application.DTOs;

public record ReportSummaryDto(
    int TotalReservations,
    int TotalNights,
    decimal TotalAmount,
    IReadOnlyList<ReportStatusSummaryDto> ByStatus,
    IReadOnlyList<ReportOwnerSummaryDto> ByOwner,
    IReadOnlyList<ReportOwnerStatusSummaryDto> ByOwnerAndStatus);
