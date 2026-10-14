using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrailCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReminderIntervalMin",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "ReminderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderNumber = table.Column<int>(type: "integer", nullable: false),
                    NutritionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderItems", x => x.Id);
                    table.CheckConstraint("CK_ReminderItems_ReminderNumber", "\"ReminderNumber\" BETWEEN 1 AND 500");
                    table.ForeignKey(
                        name: "FK_ReminderItems_NutritionItems_NutritionItemId",
                        column: x => x.NutritionItemId,
                        principalTable: "NutritionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReminderItems_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Races_ReminderIntervalMin",
                table: "Races",
                sql: "\"ReminderIntervalMin\" BETWEEN 5 AND 180");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderItems_NutritionItemId",
                table: "ReminderItems",
                column: "NutritionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderItems_RaceId_ReminderNumber_Position",
                table: "ReminderItems",
                columns: new[] { "RaceId", "ReminderNumber", "Position" });

            migrationBuilder.Sql(
                "DELETE FROM \"Items\" WHERE \"NutritionItemId\" IN (SELECT \"Id\" FROM \"NutritionItems\" WHERE \"Type\" <> 'Liquid');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReminderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Races_ReminderIntervalMin",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "ReminderIntervalMin",
                table: "Races");
        }
    }
}
