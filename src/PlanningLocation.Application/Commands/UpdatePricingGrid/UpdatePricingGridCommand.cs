using MediatR;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Commands.UpdatePricingGrid;

public record UpdatePricingGridCommand(
    int Year,
    IReadOnlyList<PricingLineInput> Lines) : IRequest;

public record PricingLineInput(ClientType ClientType, decimal PricePerDayPerPerson);
