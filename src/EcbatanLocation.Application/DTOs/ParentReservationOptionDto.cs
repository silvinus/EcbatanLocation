using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record ParentReservationOptionDto(
    Guid Id,
    string StudioName,
    string TenantName,
    DateOnly StartDate,
    DateOnly EndDate,
    ReservationStatus Status);
