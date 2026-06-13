using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record ReportOwnerStatusSummaryDto(
    string OwnerName,
    ReservationStatus Status,
    int Count,
    int TotalNights,
    decimal TotalAmount);
