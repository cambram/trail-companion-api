using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RaceDate",
                table: "Races",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaceDate",
                table: "Races");
        }
    }
}
