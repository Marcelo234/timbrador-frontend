namespace AuthBackend.Domain.Entities;

public class EstadoUsuario
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }

    private EstadoUsuario() { } // Para EF Core
}

public static class EstadosUsuario
{
    public const int Activo = 1;
    public const int Inactivo = 2;
    public const int Suspendido = 3;
}
