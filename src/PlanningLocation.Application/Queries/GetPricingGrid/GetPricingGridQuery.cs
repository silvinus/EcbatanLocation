using MediatR;
using PlanningLocation.Application.DTOs;

namespace PlanningLocation.Application.Queries.GetPricingGrid;

public record GetPricingGridQuery(int Year) : IRequest<PricingGridDto?>;
