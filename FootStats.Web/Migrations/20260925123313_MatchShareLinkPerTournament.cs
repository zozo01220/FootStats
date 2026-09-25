using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class MatchShareLinkPerTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchShareLinks_Matches_MatchRecordId",
                table: "MatchShareLinks");

            migrationBuilder.RenameColumn(
                name: "MatchRecordId",
                table: "MatchShareLinks",
                newName: "TournamentId");

            migrationBuilder.RenameIndex(
                name: "IX_MatchShareLinks_MatchRecordId",
                table: "MatchShareLinks",
                newName: "IX_MatchShareLinks_TournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchShareLinks_Tournaments_TournamentId",
                table: "MatchShareLinks",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchShareLinks_Tournaments_TournamentId",
                table: "MatchShareLinks");

            migrationBuilder.RenameColumn(
                name: "TournamentId",
                table: "MatchShareLinks",
                newName: "MatchRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_MatchShareLinks_TournamentId",
                table: "MatchShareLinks",
                newName: "IX_MatchShareLinks_MatchRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchShareLinks_Matches_MatchRecordId",
                table: "MatchShareLinks",
                column: "MatchRecordId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
