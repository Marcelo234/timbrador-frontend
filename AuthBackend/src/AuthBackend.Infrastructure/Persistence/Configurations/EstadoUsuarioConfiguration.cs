using AuthBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBackend.Infrastructure.Persistence.Configurations;

public class EstadoUsuarioConfiguration : IEntityTypeConfiguration<EstadoUsuario>
{
    public void Configure(EntityTypeBuilder<EstadoUsuario> builder)
    {
        builder.ToTable("EstadosUsuario");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Descripcion).HasMaxLength(200);

        builder.HasData(
            CreateEstado(EstadosUsuario.Activo, "Activo", "Usuario activo con acceso completo"),
            CreateEstado(EstadosUsuario.Inactivo, "Inactivo", "Usuario inactivo sin acceso"),
            CreateEstado(EstadosUsuario.Suspendido, "Suspendido", "Usuario suspendido temporalmente")
        );
    }

    private static object CreateEstado(int id, string nombre, string descripcion) =>
        new { Id = id, Nombre = nombre, Descripcion = descripcion };
}
