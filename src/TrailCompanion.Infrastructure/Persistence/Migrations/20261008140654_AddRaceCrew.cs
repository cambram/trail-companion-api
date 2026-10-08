using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    public partial class AddRaceCrew : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pin",
                table: "Races",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Races" AS r
                SET "Pin" = p."Pin"
                FROM (
                    SELECT numbered."Id", lpad(shuffled.n::text, 6, '0') AS "Pin"
                    FROM (SELECT "Id", row_number() OVER () AS rn FROM "Races") AS numbered
                    JOIN (SELECT n, row_number() OVER (ORDER BY random()) AS rn
                          FROM generate_series(0, 999999) AS n) AS shuffled
                        ON shuffled.rn = numbered.rn
                ) AS p
                WHERE r."Id" = p."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Pin",
                table: "Races",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "RaceCrewMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceCrewMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceCrewMembers_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceCrewMembers_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Races_Pin",
                table: "Races",
                column: "Pin",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Races_Pin",
                table: "Races",
                sql: "\"Pin\" ~ '^[0-9]{6}$'");

            migrationBuilder.CreateIndex(
                name: "IX_RaceCrewMembers_AppUserId",
                table: "RaceCrewMembers",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceCrewMembers_RaceId_AppUserId",
                table: "RaceCrewMembers",
                columns: new[] { "RaceId", "AppUserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaceCrewMembers");

            migrationBuilder.DropIndex(
                name: "IX_Races_Pin",
                table: "Races");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Races_Pin",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "Pin",
                table: "Races");
        }
    }
}
