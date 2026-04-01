using AuthBackend.Domain.Entities;

namespace AuthBackend.Application.DTOs;

public record UsuarioResponseDto
{
    public Guid Id { get; init; }
    public string Nombres { get; init; } = string.Empty;
    public string Apellidos { get; init; } = string.Empty;
    public string Cedula { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }

    public UsuarioResponseDto() { }

    public UsuarioResponseDto(Usuario usuario)
    {
        Id = usuario.Id;
        Nombres = usuario.Nombres;
        Apellidos = usuario.Apellidos;
        Cedula = usuario.Cedula;
        Email = usuario.Email;
        FechaCreacion = usuario.FechaCreacion;
    }
}
