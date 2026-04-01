namespace AuthBackend.Application.DTOs;

public record RecuperacionPasswordDto
{
    public string Email { get; init; } = string.Empty;
}
