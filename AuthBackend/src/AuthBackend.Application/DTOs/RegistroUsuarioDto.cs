namespace AuthBackend.Application.DTOs;

public record RegistroUsuarioDto
{
    public string Nombres { get; init; } = string.Empty;
    public string Apellidos { get; init; } = string.Empty;
    public string Cedula { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmarPassword { get; init; } = string.Empty;
}
