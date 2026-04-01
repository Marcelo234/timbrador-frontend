using AuthBackend.Domain.Entities;

namespace AuthBackend.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorEmail(string email);
    Task<Usuario?> BuscarPorId(Guid id);
    Task<bool> ExisteUsuario(string email);
    Task GuardarUsuario(Usuario usuario);
    Task ActualizarUsuario(Usuario usuario);
}
