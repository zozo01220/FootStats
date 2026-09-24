using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class MoveTeamNumberToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamNumber",
                table: "Tournaments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Un tournoi/plateau existant peut avoir des matchs enregistrés avec des numéros d'équipe
            // différents (choisis match par match avant ce changement) : on reprend le numéro le plus
            // fréquent parmi ses matchs, pour ne pas changer silencieusement l'équipe affichée.
            migrationBuilder.Sql(
                """
                UPDATE Tournaments
                SET TeamNumber = COALESCE((
                    SELECT m.TeamNumber
                    FROM Matches m
                    WHERE m.TournamentId = Tournaments.Id
                    GROUP BY m.TeamNumber
                    ORDER BY COUNT(*) DESC, m.TeamNumber ASC
                    LIMIT 1
                ), 1);
                """);

            migrationBuilder.DropColumn(
                name: "TeamNumber",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamNumber",
                table: "Tournaments");

            migrationBuilder.AddColumn<int>(
                name: "TeamNumber",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
