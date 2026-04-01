namespace AuthBackend.Application.DTOs;

public record RefreshTokenDto
{
    public string RefreshToken { get; init; } = string.Empty;
}
