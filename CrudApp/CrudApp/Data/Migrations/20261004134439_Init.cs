using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudApp.CrudApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "points_of_interest",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    VisitedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    PhotoBlobName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ForecastMinC = table.Column<double>(type: "double precision", nullable: true),
                    ForecastMaxC = table.Column<double>(type: "double precision", nullable: true),
                    ForecastPrecipitationProbability = table.Column<int>(type: "integer", nullable: true),
                    ForecastWeatherCode = table.Column<int>(type: "integer", nullable: true),
                    ForecastUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_points_of_interest", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "points_of_interest",
                schema: "public");
        }
    }
}
