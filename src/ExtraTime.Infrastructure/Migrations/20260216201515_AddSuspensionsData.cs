using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuspensionsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerSuspensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalPlayerId = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsKeyPlayer = table.Column<bool>(type: "bit", nullable: false),
                    SuspensionReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpectedReturn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSuspensions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamSuspensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalSuspended = table.Column<int>(type: "int", nullable: false),
                    KeyPlayersSuspended = table.Column<int>(type: "int", nullable: false),
                    CardSuspensions = table.Column<int>(type: "int", nullable: false),
                    DisciplinarySuspensions = table.Column<int>(type: "int", nullable: false),
                    SuspendedPlayerNames = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SuspensionImpactScore = table.Column<double>(type: "float", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextSyncDue = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSuspensions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSuspensions_TeamId_IsActive",
                table: "PlayerSuspensions",
                columns: new[] { "TeamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamSuspensions_TeamId",
                table: "TeamSuspensions",
                column: "TeamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerSuspensions");

            migrationBuilder.DropTable(
                name: "TeamSuspensions");
        }
    }
}
