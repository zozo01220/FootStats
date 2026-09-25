using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceStatus",
                table: "Tournaments",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Tournaments");
        }
    }
}
