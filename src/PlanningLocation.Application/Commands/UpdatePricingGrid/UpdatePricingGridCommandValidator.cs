using FluentValidation;

namespace PlanningLocation.Application.Commands.UpdatePricingGrid;

public class UpdatePricingGridCommandValidator : AbstractValidator<UpdatePricingGridCommand>
{
    public UpdatePricingGridCommandValidator()
    {
        RuleFor(x => x.Year).GreaterThan(2000).LessThan(2100);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ClientType).IsInEnum();
            line.RuleFor(l => l.PricePerDayPerPerson).GreaterThanOrEqualTo(0);
        });
    }
}
