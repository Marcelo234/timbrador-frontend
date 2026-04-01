using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthBackend.Infrastructure.Persistence.Repositories;

public class SesionRepository : ISesionRepository
{
    private readonly AuthDbContext _context;

    public SesionRepository(AuthDbContext context) => _context = context;

    public async Task GuardarSesion(Sesion sesion)
    {
        _context.Sesiones.Add(sesion);
        await _context.SaveChangesAsync();
    }

    public async Task<Sesion?> BuscarPorToken(string refreshToken) =>
        await _context.Sesiones.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    public async Task FinalizarSesion(string refreshToken)
    {
        var sesion = await BuscarPorToken(refreshToken);
        if (sesion != null)
        {
            sesion.Revocar();
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevocarTodasLasSesionesDelUsuario(Guid usuarioId)
    {
        var sesiones = await _context.Sesiones
            .Where(s => s.UsuarioId == usuarioId && !s.Revocado)
            .ToListAsync();

        foreach (var sesion in sesiones)
            sesion.Revocar();

        await _context.SaveChangesAsync();
    }
}
