using FluentValidation;

namespace EcbatanLocation.Application.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private static readonly string[] ValidRoles = ["Owner", "Admin"];

    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("L'identifiant utilisateur est requis.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Le nom est requis.")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis.")
            .EmailAddress().WithMessage("L'email n'est pas valide.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("Au moins un rôle est requis.")
            .Must(roles => roles.All(r => ValidRoles.Contains(r)))
            .WithMessage("Les rôles valides sont : Owner, Admin.");
    }
}
