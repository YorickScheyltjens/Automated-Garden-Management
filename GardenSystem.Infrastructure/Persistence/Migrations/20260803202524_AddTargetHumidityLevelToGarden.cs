using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetHumidityLevelToGarden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "gardens",
                columns: table => new
                {
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalSurfaceArea = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LocationDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    TargetHumidityLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gardens", x => x.GardenId);
                    table.ForeignKey(
                        name: "FK_gardens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plants",
                columns: table => new
                {
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GardenId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Species = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlantType = table.Column<int>(type: "integer", nullable: false),
                    PlantationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SurfaceAreaRequired = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IdealHumidityLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plants", x => x.PlantId);
                    table.ForeignKey(
                        name: "FK_plants_gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "gardens",
                        principalColumn: "GardenId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gardens_UserId",
                table: "gardens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_plants_GardenId",
                table: "plants",
                column: "GardenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plants");

            migrationBuilder.DropTable(
                name: "gardens");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
