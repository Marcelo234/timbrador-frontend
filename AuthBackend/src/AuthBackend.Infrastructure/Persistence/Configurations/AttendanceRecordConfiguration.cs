using AuthBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBackend.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.ActionType).IsRequired();
        builder.Property(a => a.Timestamp).IsRequired();

        // Uniqueness per (UserId, ActionType, day) is enforced at the application level
        // in the Clock endpoint. A DB-level unique index on (UserId, ActionType) alone
        // would block the same action type across different days.
        builder.HasIndex(a => new { a.UserId, a.ActionType })
               .HasDatabaseName("IX_AttendanceRecords_UserId_ActionType");

        builder.HasOne(a => a.Usuario)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
