using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record ReportStatusSummaryDto(ReservationStatus Status, int Count, decimal TotalAmount);
