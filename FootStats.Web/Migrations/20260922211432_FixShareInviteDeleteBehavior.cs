using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootStats.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixShareInviteDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareInvites_Users_ClaimedByUserId",
                table: "ShareInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareInvites_Users_CreatedByAdminId",
                table: "ShareInvites");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByAdminId",
                table: "ShareInvites",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_ShareInvites_Users_ClaimedByUserId",
                table: "ShareInvites",
                column: "ClaimedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareInvites_Users_CreatedByAdminId",
                table: "ShareInvites",
                column: "CreatedByAdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareInvites_Users_ClaimedByUserId",
                table: "ShareInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareInvites_Users_CreatedByAdminId",
                table: "ShareInvites");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByAdminId",
                table: "ShareInvites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareInvites_Users_ClaimedByUserId",
                table: "ShareInvites",
                column: "ClaimedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareInvites_Users_CreatedByAdminId",
                table: "ShareInvites",
                column: "CreatedByAdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
