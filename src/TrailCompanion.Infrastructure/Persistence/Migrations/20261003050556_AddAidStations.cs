using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAidStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AidStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kit = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AidStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AidStations_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AidStationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AidStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    NutritionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AidStationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AidStationItems_AidStations_AidStationId",
                        column: x => x.AidStationId,
                        principalTable: "AidStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AidStationItems_NutritionItems_NutritionItemId",
                        column: x => x.NutritionItemId,
                        principalTable: "NutritionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AidStationItems_AidStationId",
                table: "AidStationItems",
                column: "AidStationId");

            migrationBuilder.CreateIndex(
                name: "IX_AidStationItems_NutritionItemId",
                table: "AidStationItems",
                column: "NutritionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AidStations_SegmentId",
                table: "AidStations",
                column: "SegmentId");

            migrationBuilder.Sql(
                "ALTER TABLE \"AidStations\" ADD CONSTRAINT \"UQ_AidStations_SegmentId\" UNIQUE (\"SegmentId\") DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AidStationItems");

            migrationBuilder.DropTable(
                name: "AidStations");
        }
    }
}
