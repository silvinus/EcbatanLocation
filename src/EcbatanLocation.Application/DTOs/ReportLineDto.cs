using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record ReportLineDto(
    Guid ReservationId,
    string StudioName,
    string OwnerName,
    string TenantName,
    DateOnly StartDate,
    DateOnly EndDate,
    int NumberOfDays,
    IReadOnlyList<ReportPersonLineDto> PersonLines,
    decimal? TotalAmount,
    ReservationStatus Status);
