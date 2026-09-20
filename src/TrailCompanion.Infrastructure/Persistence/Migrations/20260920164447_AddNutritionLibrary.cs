using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Carbs = table.Column<int>(type: "integer", nullable: true),
                    Calories = table.Column<int>(type: "integer", nullable: true),
                    Caffeine = table.Column<int>(type: "integer", nullable: true),
                    Sodium = table.Column<int>(type: "integer", nullable: true),
                    Potassium = table.Column<int>(type: "integer", nullable: true),
                    Magnesium = table.Column<int>(type: "integer", nullable: true),
                    Calcium = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionItems", x => x.Id);
                    table.CheckConstraint("CK_NutritionItems_Caffeine", "\"Caffeine\" IS NULL OR \"Caffeine\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Calcium", "\"Calcium\" IS NULL OR \"Calcium\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Calories", "\"Calories\" IS NULL OR \"Calories\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Carbs", "\"Carbs\" IS NULL OR \"Carbs\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Magnesium", "\"Magnesium\" IS NULL OR \"Magnesium\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Potassium", "\"Potassium\" IS NULL OR \"Potassium\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Size", "\"Size\" IS NULL OR \"Size\" >= 0");
                    table.CheckConstraint("CK_NutritionItems_Sodium", "\"Sodium\" IS NULL OR \"Sodium\" >= 0");
                    table.ForeignKey(
                        name: "FK_NutritionItems_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionItems_AppUserId_Name",
                table: "NutritionItems",
                columns: new[] { "AppUserId", "Name" },
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NutritionItemId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                INSERT INTO ""NutritionItems""
                    (""Id"", ""AppUserId"", ""Name"", ""Type"", ""Carbs"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                SELECT gen_random_uuid(), owner.""AppUserId"", owner.""Name"", 'Gel', owner.""Carbs"", NOW(), NOW()
                FROM (
                    SELECT DISTINCT r.""AppUserId"", i.""Name"", i.""Carbs""
                    FROM ""Items"" i
                    JOIN ""Segments"" s ON s.""Id"" = i.""SegmentId""
                    JOIN ""Races"" r ON r.""Id"" = s.""RaceId""
                ) owner;");

            migrationBuilder.Sql(@"
                UPDATE ""Items"" i
                SET ""NutritionItemId"" = n.""Id""
                FROM ""Segments"" s, ""Races"" r, ""NutritionItems"" n
                WHERE s.""Id"" = i.""SegmentId""
                  AND r.""Id"" = s.""RaceId""
                  AND n.""AppUserId"" = r.""AppUserId""
                  AND n.""Name"" = i.""Name""
                  AND n.""Carbs"" IS NOT DISTINCT FROM i.""Carbs"";");

            migrationBuilder.AlterColumn<Guid>(
                name: "NutritionItemId",
                table: "Items",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Items_Carbs",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Carbs",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Items_NutritionItemId",
                table: "Items",
                column: "NutritionItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_NutritionItems_NutritionItemId",
                table: "Items",
                column: "NutritionItemId",
                principalTable: "NutritionItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_NutritionItems_NutritionItemId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "NutritionItems");

            migrationBuilder.DropIndex(
                name: "IX_Items_NutritionItemId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "NutritionItemId",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "Carbs",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Items_Carbs",
                table: "Items",
                sql: "\"Carbs\" >= 0");
        }
    }
}
