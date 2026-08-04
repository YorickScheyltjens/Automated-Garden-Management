using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plant_states",
                columns: table => new
                {
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentHumidityLevel = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LastIrrigationStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastIrrigationEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrentlyIrrigating = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant_states", x => x.PlantId);
                    table.ForeignKey(
                        name: "FK_plant_states_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "PlantId",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plant_states");
        }
    }
}
