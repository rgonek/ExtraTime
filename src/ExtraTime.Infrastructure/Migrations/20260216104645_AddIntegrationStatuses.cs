using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Health = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastSuccessfulSync = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptedSync = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailedSync = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    TotalFailures24h = table.Column<int>(type: "int", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastErrorDetails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DataFreshAsOf = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StaleThreshold = table.Column<TimeSpan>(type: "time", nullable: false),
                    SuccessfulSyncs24h = table.Column<int>(type: "int", nullable: false),
                    AverageSyncDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsManuallyDisabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisabledReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStatuses_IntegrationName",
                table: "IntegrationStatuses",
                column: "IntegrationName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationStatuses");
        }
    }
}
