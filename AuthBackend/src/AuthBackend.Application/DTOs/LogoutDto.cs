namespace AuthBackend.Application.DTOs;

public record LogoutDto
{
    public string RefreshToken { get; init; } = string.Empty;
}
