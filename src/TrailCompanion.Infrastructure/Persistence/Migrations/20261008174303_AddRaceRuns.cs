using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaceRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceRuns_AppUsers_StartedBy",
                        column: x => x.StartedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RaceRuns_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Arrivals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    AidStationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArrivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arrivals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arrivals_AidStations_AidStationId",
                        column: x => x.AidStationId,
                        principalTable: "AidStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Arrivals_AppUsers_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Arrivals_RaceRuns_RaceRunId",
                        column: x => x.RaceRunId,
                        principalTable: "RaceRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Arrivals_AidStationId",
                table: "Arrivals",
                column: "AidStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Arrivals_RaceRunId_AidStationId",
                table: "Arrivals",
                columns: new[] { "RaceRunId", "AidStationId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_Arrivals_RecordedBy",
                table: "Arrivals",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRuns_RaceId",
                table: "RaceRuns",
                column: "RaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceRuns_StartedBy",
                table: "RaceRuns",
                column: "StartedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Arrivals");

            migrationBuilder.DropTable(
                name: "RaceRuns");
        }
    }
}
