using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthBackend.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AuthDbContext _context;

    public UsuarioRepository(AuthDbContext context) => _context = context;

    public async Task<Usuario?> BuscarPorEmail(string email) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<Usuario?> BuscarPorId(Guid id) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> ExisteUsuario(string email) =>
        await _context.Usuarios.AnyAsync(u => u.Email == email.ToLowerInvariant());

    public async Task GuardarUsuario(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarUsuario(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();
    }
}
