namespace AuthBackend.Domain.Entities;

public class Sesion
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime FechaCreacion { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public bool Revocado { get; private set; }
    public DateTime? FechaRevocacion { get; private set; }

    private Sesion() { } // Para EF Core

    public Sesion(Guid usuarioId, string refreshToken, DateTime fechaExpiracion)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        RefreshToken = refreshToken;
        FechaCreacion = DateTime.UtcNow;
        FechaExpiracion = fechaExpiracion;
        Revocado = false;
    }

    public void Revocar()
    {
        Revocado = true;
        FechaRevocacion = DateTime.UtcNow;
    }

    public bool EstaVigente() => !Revocado && DateTime.UtcNow < FechaExpiracion;
}
