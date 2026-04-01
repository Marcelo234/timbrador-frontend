using AuthBackend.Domain.Entities;

namespace AuthBackend.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerarAccessToken(Usuario usuario);
    string GenerarRefreshToken();
}
