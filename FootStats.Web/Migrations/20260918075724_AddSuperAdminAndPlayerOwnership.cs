using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminAndPlayerOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerAdminId",
                table: "Players",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_OwnerAdminId",
                table: "Players",
                column: "OwnerAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Users_OwnerAdminId",
                table: "Players",
                column: "OwnerAdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Promeut le(s) admin(s) existant(s) (Role=0) en SuperAdmin (Role=2) : avant cette migration,
            // "Admin" était le rôle le plus élevé, il devient donc le SuperAdmin de l'app existante.
            migrationBuilder.Sql("UPDATE Users SET Role = 2 WHERE Role = 0;");

            // Rattache les joueurs déjà en base (créés avant la notion de famille) au premier SuperAdmin,
            // pour qu'ils restent visibles ; l'app peut ensuite les réassigner à un Admin si besoin.
            migrationBuilder.Sql(
                "UPDATE Players SET OwnerAdminId = (SELECT Id FROM Users WHERE Role = 2 ORDER BY Id LIMIT 1) " +
                "WHERE OwnerAdminId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Users_OwnerAdminId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_OwnerAdminId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OwnerAdminId",
                table: "Players");
        }
    }
}
