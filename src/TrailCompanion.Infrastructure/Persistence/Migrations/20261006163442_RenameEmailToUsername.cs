using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmailToUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AppUsers",
                newName: "Username");

            migrationBuilder.RenameIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                newName: "IX_AppUsers_Username");

            migrationBuilder.Sql(@"UPDATE ""AppUsers"" SET ""Username"" = split_part(""Username"", '@', 1)");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AppUsers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AppUsers",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.RenameIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                newName: "IX_AppUsers_Email");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "AppUsers",
                newName: "Email");
        }
    }
}
