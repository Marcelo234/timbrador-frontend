namespace AuthBackend.Domain.Entities;

public class TokenRecuperacion
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime FechaCreacion { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public bool Usado { get; private set; }
    public DateTime? FechaUso { get; private set; }

    private TokenRecuperacion() { } // Para EF Core

    public TokenRecuperacion(Guid usuarioId, string token)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        Token = token;
        FechaCreacion = DateTime.UtcNow;
        FechaExpiracion = DateTime.UtcNow.AddHours(1);
        Usado = false;
    }

    public void MarcarComoUsado()
    {
        Usado = true;
        FechaUso = DateTime.UtcNow;
    }

    public bool EsValido() => !Usado && DateTime.UtcNow < FechaExpiracion;
}
