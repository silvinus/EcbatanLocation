using FluentValidation;

namespace EcbatanLocation.Application.Commands.ConfirmReservation;

public class ConfirmReservationCommandValidator : AbstractValidator<ConfirmReservationCommand>
{
    public ConfirmReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.ConfirmedBy).NotEmpty().MaximumLength(200);
    }
}
