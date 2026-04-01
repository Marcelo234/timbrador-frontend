using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceDailyUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite supports unique indexes on expressions via raw SQL.
            // This prevents the same ActionType being recorded twice for the
            // same user on the same UTC day, even if two requests race past
            // the application-level check simultaneously.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS
                    IX_AttendanceRecords_UserId_ActionType_Day
                ON AttendanceRecords (UserId, ActionType, date(Timestamp));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_AttendanceRecords_UserId_ActionType_Day;
            ");
        }
    }
}
