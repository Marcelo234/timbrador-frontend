using AuthBackend.Application.DTOs;
using FluentValidation;

namespace AuthBackend.Application.Validators;

public class RestablecerPasswordValidator : AbstractValidator<RestablecerPasswordDto>
{
    public RestablecerPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token es requerido");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El email no tiene un formato válido");

        RuleFor(x => x.NuevaPassword)
            .DebeSerPasswordSegura();
    }
}
