using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.Commands.UpdatePricingGrid;

public record UpdatePricingGridCommand(
    int Year,
    IReadOnlyList<PricingLineInput> Lines) : IRequest, IRequireAdmin;

public record PricingLineInput(ClientType ClientType, decimal PricePerDayPerPerson);
