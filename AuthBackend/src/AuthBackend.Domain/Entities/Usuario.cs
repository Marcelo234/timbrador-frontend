namespace AuthBackend.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nombres { get; private set; } = string.Empty;
    public string Apellidos { get; private set; } = string.Empty;
    public string Cedula { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public int EstadoId { get; private set; }
    public EstadoUsuario? Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    private Usuario() { } // Para EF Core

    public Usuario(string nombres, string apellidos, string cedula, string email, string passwordHash, int estadoId = EstadosUsuario.Activo)
    {
        Id = Guid.NewGuid();
        Nombres = nombres;
        Apellidos = apellidos;
        Cedula = cedula;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        EstadoId = estadoId;
        FechaCreacion = DateTime.UtcNow;
    }

    public void ActualizarPassword(string nuevoPasswordHash)
    {
        PasswordHash = nuevoPasswordHash;
        FechaActualizacion = DateTime.UtcNow;
    }

    public bool EstaActivo() => EstadoId == EstadosUsuario.Activo;
}
