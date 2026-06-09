using MediatR;
using PlanningLocation.Application.Behaviors;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Commands.UpdatePricingGrid;

public record UpdatePricingGridCommand(
    int Year,
    IReadOnlyList<PricingLineInput> Lines) : IRequest, IRequireAuthorization;

public record PricingLineInput(ClientType ClientType, decimal PricePerDayPerPerson);
