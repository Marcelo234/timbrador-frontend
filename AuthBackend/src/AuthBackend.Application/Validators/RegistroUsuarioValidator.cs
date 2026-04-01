using AuthBackend.Application.DTOs;
using FluentValidation;

namespace AuthBackend.Application.Validators;

public class RegistroUsuarioValidator : AbstractValidator<RegistroUsuarioDto>
{
    public RegistroUsuarioValidator()
    {
        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son requeridos")
            .MaximumLength(100).WithMessage("Los apellidos no pueden exceder 100 caracteres");

        RuleFor(x => x.Cedula)
            .NotEmpty().WithMessage("La cédula es requerida")
            .Matches(@"^\d{10}$").WithMessage("La cédula debe tener 10 dígitos");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El email no tiene un formato válido");

        RuleFor(x => x.Password)
            .DebeSerPasswordSegura();

        RuleFor(x => x.ConfirmarPassword)
            .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden");
    }
}
