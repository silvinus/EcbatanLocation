using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.DTOs;

public record PricingLineDto(ClientType ClientType, decimal PricePerDayPerPerson);
