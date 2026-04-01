using AuthBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBackend.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nombres).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Apellidos).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Cedula).IsRequired().HasMaxLength(10);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FechaCreacion).IsRequired();
        builder.Property(u => u.EstadoId).IsRequired().HasDefaultValue(EstadosUsuario.Activo);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Cedula).IsUnique();

        builder.HasOne(u => u.Estado)
            .WithMany()
            .HasForeignKey(u => u.EstadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
