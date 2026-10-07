using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenSessionStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartedAtUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(@"UPDATE ""RefreshTokens"" SET ""SessionStartedAtUtc"" = ""CreatedAtUtc""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionStartedAtUtc",
                table: "RefreshTokens");
        }
    }
}
