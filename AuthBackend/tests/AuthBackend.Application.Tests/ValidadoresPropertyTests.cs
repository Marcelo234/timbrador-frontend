using AuthBackend.Application.DTOs;
using AuthBackend.Application.Validators;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuthBackend.Application.Tests;

// Feature: sistema-autenticacion — Property tests para validadores
public class ValidadoresPropertyTests
{
    private readonly RegistroUsuarioValidator _validator = new();

    // Property 3: Cualquier string sin '@' debe fallar validación de email
    [Property(MaxTest = 100)]
    public Property Email_SinArroba_FallaValidacion()
    {
        return Prop.ForAll(
            ArbMap.Default.ArbFor<string>().Filter(s => s != null && !s.Contains('@') && s.Length > 0),
            emailInvalido =>
            {
                var dto = new RegistroUsuarioDto
                {
                    Nombres = "Juan",
                    Apellidos = "Pérez",
                    Cedula = "1234567890",
                    Email = emailInvalido,
                    Password = "Password123",
                    ConfirmarPassword = "Password123"
                };
                var result = _validator.Validate(dto);
                return result.Errors.Any(e => e.PropertyName == "Email");
            });
    }

    // Property 4: Contraseña sin mayúscula debe fallar
    [Fact]
    public void Password_SinMayuscula_FallaValidacion()
    {
        var dto = ValidDto() with { Password = "password123", ConfirmarPassword = "password123" };
        var result = _validator.Validate(dto);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    // Property 4: Contraseña sin minúscula debe fallar
    [Fact]
    public void Password_SinMinuscula_FallaValidacion()
    {
        var dto = ValidDto() with { Password = "PASSWORD123", ConfirmarPassword = "PASSWORD123" };
        var result = _validator.Validate(dto);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    // Property 4: Contraseña sin número debe fallar
    [Fact]
    public void Password_SinNumero_FallaValidacion()
    {
        var dto = ValidDto() with { Password = "PasswordABC", ConfirmarPassword = "PasswordABC" };
        var result = _validator.Validate(dto);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    // Property 4: Contraseña menor a 8 caracteres debe fallar
    [Fact]
    public void Password_MenorOchoCaracteres_FallaValidacion()
    {
        var dto = ValidDto() with { Password = "Pa1", ConfirmarPassword = "Pa1" };
        var result = _validator.Validate(dto);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    // DTO válido no debe tener errores
    [Fact]
    public void DtoValido_NoTieneErrores()
    {
        var dto = ValidDto();
        var result = _validator.Validate(dto);
        Assert.True(result.IsValid);
    }

    // Cédula con menos de 10 dígitos debe fallar
    [Fact]
    public void Cedula_MenosDiezDigitos_FallaValidacion()
    {
        var dto = ValidDto() with { Cedula = "12345" };
        var result = _validator.Validate(dto);
        Assert.Contains(result.Errors, e => e.PropertyName == "Cedula");
    }

    // Contraseñas que no coinciden deben fallar
    [Fact]
    public void ConfirmarPassword_NoCoincide_FallaValidacion()
    {
        var dto = ValidDto() with { ConfirmarPassword = "OtraPassword123" };
        var result = _validator.Validate(dto);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConfirmarPassword");
    }

    private static RegistroUsuarioDto ValidDto() => new()
    {
        Nombres = "Juan",
        Apellidos = "Pérez",
        Cedula = "1234567890",
        Email = "juan@example.com",
        Password = "Password123",
        ConfirmarPassword = "Password123"
    };
}
