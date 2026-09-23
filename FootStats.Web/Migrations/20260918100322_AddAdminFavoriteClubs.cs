using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFavoriteClubs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminFavoriteClubs",
                columns: table => new
                {
                    FavoriteClubsId = table.Column<int>(type: "INTEGER", nullable: false),
                    FavoritedByAdminsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminFavoriteClubs", x => new { x.FavoriteClubsId, x.FavoritedByAdminsId });
                    table.ForeignKey(
                        name: "FK_AdminFavoriteClubs_Clubs_FavoriteClubsId",
                        column: x => x.FavoriteClubsId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminFavoriteClubs_Users_FavoritedByAdminsId",
                        column: x => x.FavoritedByAdminsId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminFavoriteClubs_FavoritedByAdminsId",
                table: "AdminFavoriteClubs",
                column: "FavoritedByAdminsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminFavoriteClubs");
        }
    }
}
