namespace AuthBackend.Application.DTOs;

public record RestablecerPasswordDto
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string NuevaPassword { get; init; } = string.Empty;
}
