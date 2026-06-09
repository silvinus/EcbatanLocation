using FluentValidation;

namespace PlanningLocation.Application.Commands.ConfirmReservation;

public class ConfirmReservationCommandValidator : AbstractValidator<ConfirmReservationCommand>
{
    public ConfirmReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.ConfirmedBy).NotEmpty().MaximumLength(200);
    }
}
