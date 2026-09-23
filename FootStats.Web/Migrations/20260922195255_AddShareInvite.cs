using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddShareInvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShareInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedByAdminId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecipientEmailHint = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareInvites_Users_ClaimedByUserId",
                        column: x => x.ClaimedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareInvites_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShareInvitePlayers",
                columns: table => new
                {
                    PlayersId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShareInviteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareInvitePlayers", x => new { x.PlayersId, x.ShareInviteId });
                    table.ForeignKey(
                        name: "FK_ShareInvitePlayers_Players_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareInvitePlayers_ShareInvites_ShareInviteId",
                        column: x => x.ShareInviteId,
                        principalTable: "ShareInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShareInvitePlayers_ShareInviteId",
                table: "ShareInvitePlayers",
                column: "ShareInviteId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareInvites_ClaimedByUserId",
                table: "ShareInvites",
                column: "ClaimedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareInvites_CreatedByAdminId",
                table: "ShareInvites",
                column: "CreatedByAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShareInvitePlayers");

            migrationBuilder.DropTable(
                name: "ShareInvites");
        }
    }
}
