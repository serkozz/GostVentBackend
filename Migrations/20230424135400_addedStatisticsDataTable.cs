using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GostVentBackend.Migrations
{
    /// <inheritdoc />
    public partial class addedStatisticsDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatisticsReport",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsDataSingle = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticsReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatisticsData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<float>(type: "REAL", nullable: false),
                    StatisticsReportId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticsData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatisticsData_StatisticsReport_StatisticsReportId",
                        column: x => x.StatisticsReportId,
                        principalTable: "StatisticsReport",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsData_StatisticsReportId",
                table: "StatisticsData",
                column: "StatisticsReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatisticsData");

            migrationBuilder.DropTable(
                name: "StatisticsReport");
        }
    }
}
