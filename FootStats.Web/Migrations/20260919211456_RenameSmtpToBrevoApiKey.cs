using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class RenameSmtpToBrevoApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AppPasswordEncrypted",
                table: "SmtpSettings",
                newName: "ApiKeyEncrypted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApiKeyEncrypted",
                table: "SmtpSettings",
                newName: "AppPasswordEncrypted");
        }
    }
}
