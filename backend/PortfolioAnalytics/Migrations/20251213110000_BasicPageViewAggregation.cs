using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PortfolioAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class BasicPageViewAggregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BasicPageViewAggregates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasicPageViewAggregates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BasicPageViewAggregates_Date_Path_DeviceCategory",
                table: "BasicPageViewAggregates",
                columns: new[] { "Date", "Path", "DeviceCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BasicPageViewAggregates_LastSeenAt",
                table: "BasicPageViewAggregates",
                column: "LastSeenAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasicPageViewAggregates");
        }
    }
}
