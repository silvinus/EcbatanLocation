using EcbatanLocation.Domain.Enums;
using FluentValidation;

namespace EcbatanLocation.Application.Commands.UpdateStudio;

public class UpdateStudioCommandValidator : AbstractValidator<UpdateStudioCommand>
{
    public UpdateStudioCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);

        When(x => x.RentalMode == RentalMode.PerBed, () =>
        {
            RuleFor(x => x.NumberOfBeds)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Un logement loué au lit doit avoir au moins un lit.");
            RuleFor(x => x.NumberOfBeds)
                .LessThanOrEqualTo(x => x.Capacity)
                .WithMessage("Le nombre de lits ne peut pas dépasser la capacité.");
        });
    }
}
