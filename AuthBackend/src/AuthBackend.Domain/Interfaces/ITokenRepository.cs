using AuthBackend.Domain.Entities;

namespace AuthBackend.Domain.Interfaces;

public interface ITokenRepository
{
    Task GuardarToken(TokenRecuperacion token);
    Task<TokenRecuperacion?> BuscarToken(string token);
    Task InvalidarToken(string token);
}
