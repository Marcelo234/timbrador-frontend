using AuthBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBackend.Infrastructure.Persistence.Configurations;

public class SesionConfiguration : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> builder)
    {
        builder.ToTable("Sesiones");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RefreshToken).IsRequired().HasMaxLength(500);
        builder.Property(s => s.FechaCreacion).IsRequired();
        builder.Property(s => s.FechaExpiracion).IsRequired();
        builder.Property(s => s.Revocado).IsRequired();

        builder.HasIndex(s => s.RefreshToken).IsUnique();
        builder.HasIndex(s => s.UsuarioId);
    }
}
