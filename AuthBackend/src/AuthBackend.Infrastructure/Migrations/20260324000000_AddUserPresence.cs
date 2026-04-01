using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPresence",
                columns: table => new
                {
                    UserId      = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPresence", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPresence_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserPresence");
        }
    }
}
