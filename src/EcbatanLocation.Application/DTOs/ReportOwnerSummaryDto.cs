namespace EcbatanLocation.Application.DTOs;

public record ReportOwnerSummaryDto(string OwnerName, int Count, int TotalNights, decimal TotalAmount);
