using AuthBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBackend.Infrastructure.Persistence.Configurations;

public class TokenRecuperacionConfiguration : IEntityTypeConfiguration<TokenRecuperacion>
{
    public void Configure(EntityTypeBuilder<TokenRecuperacion> builder)
    {
        builder.ToTable("TokensRecuperacion");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Token).IsRequired().HasMaxLength(200);
        builder.Property(t => t.FechaCreacion).IsRequired();
        builder.Property(t => t.FechaExpiracion).IsRequired();
        builder.Property(t => t.Usado).IsRequired();

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UsuarioId);
    }
}
