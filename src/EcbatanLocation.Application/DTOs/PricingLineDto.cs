using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record PricingLineDto(ClientType ClientType, decimal PricePerDayPerPerson);
