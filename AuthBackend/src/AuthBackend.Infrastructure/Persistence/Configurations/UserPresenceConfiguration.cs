using AuthBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBackend.Infrastructure.Persistence.Configurations;

public class UserPresenceConfiguration : IEntityTypeConfiguration<UserPresence>
{
    public void Configure(EntityTypeBuilder<UserPresence> builder)
    {
        builder.ToTable("UserPresence");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.LastSeenUtc).IsRequired();

        builder.HasOne(p => p.Usuario)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
