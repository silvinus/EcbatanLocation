using FluentValidation;

namespace EcbatanLocation.Application.Commands.UpdateReservation;

public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
{
    public UpdateReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PersonLines).NotEmpty()
            .WithMessage("At least one person line is required.");
        RuleForEach(x => x.PersonLines).ChildRules(line =>
        {
            line.RuleFor(l => l.ClientType).IsInEnum();
            line.RuleFor(l => l.AdultCount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.ChildrenUnder3Count).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.PersonLines)
            .Must(lines => lines.Sum(l => l.AdultCount) >= 1)
            .WithMessage("At least one adult is required across all person lines.");
    }
}
