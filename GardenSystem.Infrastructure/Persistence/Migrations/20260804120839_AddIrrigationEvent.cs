using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIrrigationEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "irrigation_events",
                columns: table => new
                {
                    IrrigationEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HumidityBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HumidityAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irrigation_events", x => x.IrrigationEventId);
                    table.ForeignKey(
                        name: "FK_irrigation_events_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "PlantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_irrigation_events_PlantId_StartTimeUtc",
                table: "irrigation_events",
                columns: new[] { "PlantId", "StartTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "irrigation_events");
        }
    }
}
