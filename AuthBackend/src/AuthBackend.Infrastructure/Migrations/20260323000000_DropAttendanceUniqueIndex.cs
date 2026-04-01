using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropAttendanceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the unique constraint that incorrectly prevents the same
            // ActionType from being recorded on different calendar days.
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_UserId_ActionType",
                table: "AttendanceRecords");

            // Re-create as a non-unique index for query performance only.
            // Day-level uniqueness is enforced at the application layer in
            // AttendanceController.Clock().
            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UserId_ActionType",
                table: "AttendanceRecords",
                columns: new[] { "UserId", "ActionType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_UserId_ActionType",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UserId_ActionType",
                table: "AttendanceRecords",
                columns: new[] { "UserId", "ActionType" },
                unique: true);
        }
    }
}
