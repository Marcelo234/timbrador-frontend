using AuthBackend.Domain.Entities;

namespace AuthBackend.Domain.Interfaces;

public interface ISesionRepository
{
    Task GuardarSesion(Sesion sesion);
    Task<Sesion?> BuscarPorToken(string token);
    Task FinalizarSesion(string token);
    Task RevocarTodasLasSesionesDelUsuario(Guid usuarioId);
}
