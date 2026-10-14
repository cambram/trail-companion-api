using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentAscent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AscentM",
                table: "Segments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Segments_AscentM",
                table: "Segments",
                sql: "\"AscentM\" IS NULL OR (\"AscentM\" >= 0 AND \"AscentM\" <= 10000)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Segments_AscentM",
                table: "Segments");

            migrationBuilder.DropColumn(
                name: "AscentM",
                table: "Segments");
        }
    }
}
