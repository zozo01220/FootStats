using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSportAndEquestrian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Sport",
                table: "Seasons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EquestrianCompetitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SeasonId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquestrianCompetitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquestrianCompetitions_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquestrianEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Discipline = table.Column<string>(type: "TEXT", nullable: false),
                    Ranking = table.Column<string>(type: "TEXT", nullable: false),
                    CompetitionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquestrianEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquestrianEntries_EquestrianCompetitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "EquestrianCompetitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquestrianCompetitions_SeasonId",
                table: "EquestrianCompetitions",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_EquestrianEntries_CompetitionId",
                table: "EquestrianEntries",
                column: "CompetitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquestrianEntries");

            migrationBuilder.DropTable(
                name: "EquestrianCompetitions");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "Seasons");
        }
    }
}
