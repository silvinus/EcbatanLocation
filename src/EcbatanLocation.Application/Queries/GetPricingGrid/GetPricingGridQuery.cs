using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetPricingGrid;

public record GetPricingGridQuery(int Year) : IRequest<PricingGridDto?>;
