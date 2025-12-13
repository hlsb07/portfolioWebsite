using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PortfolioAnalytics.Migrations
{
    /// <inheritdoc />
    public partial class SessionBasedTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyAggregates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    TotalVisits = table.Column<int>(type: "integer", nullable: false),
                    AvgScrollDepthPercent = table.Column<double>(type: "double precision", nullable: false),
                    AvgSessionDurationMs = table.Column<double>(type: "double precision", nullable: false),
                    AvgTimeOnPageMs = table.Column<double>(type: "double precision", nullable: false),
                    DesktopSessions = table.Column<int>(type: "integer", nullable: false),
                    MobileSessions = table.Column<int>(type: "integer", nullable: false),
                    TabletSessions = table.Column<int>(type: "integer", nullable: false),
                    BrowserBreakdownJson = table.Column<string>(type: "jsonb", nullable: true),
                    BounceCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    SectionMetricsJson = table.Column<string>(type: "jsonb", nullable: true),
                    AggregatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BrowserFamily = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyAggregates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    TotalVisits = table.Column<int>(type: "integer", nullable: false),
                    AvgScrollDepthPercent = table.Column<double>(type: "double precision", nullable: false),
                    AvgSessionDurationMs = table.Column<double>(type: "double precision", nullable: false),
                    AvgTimeOnPageMs = table.Column<double>(type: "double precision", nullable: false),
                    DesktopSessions = table.Column<int>(type: "integer", nullable: false),
                    MobileSessions = table.Column<int>(type: "integer", nullable: false),
                    TabletSessions = table.Column<int>(type: "integer", nullable: false),
                    BrowserBreakdownJson = table.Column<string>(type: "jsonb", nullable: true),
                    BounceCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    SectionMetricsJson = table.Column<string>(type: "jsonb", nullable: true),
                    AggregatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    Page = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Referrer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScrollEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitId = table.Column<int>(type: "integer", nullable: false),
                    ScrollDepthPercent = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrollEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrollEvents_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitId = table.Column<int>(type: "integer", nullable: false),
                    SectionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionEvents_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyAggregates_Date",
                table: "DailyAggregates",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrollEvents_Timestamp",
                table: "ScrollEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ScrollEvents_VisitId",
                table: "ScrollEvents",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionEvents_SectionName",
                table: "SectionEvents",
                column: "SectionName");

            migrationBuilder.CreateIndex(
                name: "IX_SectionEvents_Timestamp",
                table: "SectionEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SectionEvents_VisitId",
                table: "SectionEvents",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatedAt",
                table: "Sessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAt",
                table: "Sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionId",
                table: "Sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_SessionId",
                table: "Visits",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_Timestamp",
                table: "Visits",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyAggregates_Year_WeekNumber",
                table: "WeeklyAggregates",
                columns: new[] { "Year", "WeekNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAggregates");

            migrationBuilder.DropTable(
                name: "ScrollEvents");

            migrationBuilder.DropTable(
                name: "SectionEvents");

            migrationBuilder.DropTable(
                name: "WeeklyAggregates");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "Sessions");
        }
    }
}
