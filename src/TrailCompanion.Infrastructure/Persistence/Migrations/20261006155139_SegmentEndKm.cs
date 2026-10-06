using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SegmentEndKm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Segments_Distance",
                table: "Segments");

            migrationBuilder.RenameColumn(
                name: "Distance",
                table: "Segments",
                newName: "EndKm");

            migrationBuilder.Sql("""
                UPDATE "Segments" SET "EndKm" = NULL WHERE "EndKm" = 0;
                UPDATE "Segments" AS s SET "EndKm" = ROUND(c."Total"::numeric, 2)::double precision
                FROM (
                    SELECT "Id", SUM("EndKm") OVER (PARTITION BY "RaceId" ORDER BY "Position", "Id") AS "Total"
                    FROM "Segments"
                ) AS c
                WHERE s."Id" = c."Id" AND s."EndKm" IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Segments_EndKm",
                table: "Segments",
                sql: "\"EndKm\" IS NULL OR \"EndKm\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Segments_EndKm",
                table: "Segments");

            migrationBuilder.Sql("""
                UPDATE "Segments" AS s SET "EndKm" = GREATEST(ROUND((s."EndKm" - COALESCE(c."Previous", 0))::numeric, 2)::double precision, 0)
                FROM (
                    SELECT "Id", MAX("EndKm") OVER (PARTITION BY "RaceId" ORDER BY "Position", "Id" ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING) AS "Previous"
                    FROM "Segments"
                ) AS c
                WHERE s."Id" = c."Id" AND s."EndKm" IS NOT NULL;
                """);

            migrationBuilder.RenameColumn(
                name: "EndKm",
                table: "Segments",
                newName: "Distance");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Segments_Distance",
                table: "Segments",
                sql: "\"Distance\" IS NULL OR \"Distance\" >= 0");
        }
    }
}
