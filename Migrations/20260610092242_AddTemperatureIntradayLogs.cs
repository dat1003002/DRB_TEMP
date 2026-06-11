using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DRB_TEMP.Migrations
{
    /// <inheritdoc />
    public partial class AddTemperatureIntradayLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemperatureIntradayLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhietDo = table.Column<double>(type: "float", nullable: true),
                    DoAm = table.Column<double>(type: "float", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureIntradayLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemperatureIntradayLogs");
        }
    }
}
