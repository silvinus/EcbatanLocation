using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetPricingGrid;

public record GetPricingGridQuery(int Year) : IRequest<PricingGridDto?>;
