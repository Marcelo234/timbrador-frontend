using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthBackend.Infrastructure.Persistence.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly AuthDbContext _context;

    public TokenRepository(AuthDbContext context) => _context = context;

    public async Task GuardarToken(TokenRecuperacion token)
    {
        _context.TokensRecuperacion.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<TokenRecuperacion?> BuscarToken(string token) =>
        await _context.TokensRecuperacion.FirstOrDefaultAsync(t => t.Token == token);

    public async Task InvalidarToken(string token)
    {
        var tokenRec = await BuscarToken(token);
        if (tokenRec != null)
        {
            tokenRec.MarcarComoUsado();
            await _context.SaveChangesAsync();
        }
    }
}
