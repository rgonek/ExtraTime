using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnderstatXgData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchXgStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HomeXg = table.Column<double>(type: "float", nullable: false),
                    HomeShots = table.Column<int>(type: "int", nullable: false),
                    HomeShotsOnTarget = table.Column<int>(type: "int", nullable: false),
                    AwayXg = table.Column<double>(type: "float", nullable: false),
                    AwayShots = table.Column<int>(type: "int", nullable: false),
                    AwayShotsOnTarget = table.Column<int>(type: "int", nullable: false),
                    HomeXgWin = table.Column<bool>(type: "bit", nullable: false),
                    ActualHomeWin = table.Column<bool>(type: "bit", nullable: false),
                    XgMatchedResult = table.Column<bool>(type: "bit", nullable: false),
                    UnderstatMatchId = table.Column<int>(type: "int", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchXgStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchXgStats_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamXgStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    XgFor = table.Column<double>(type: "float", nullable: false),
                    XgAgainst = table.Column<double>(type: "float", nullable: false),
                    XgDiff = table.Column<double>(type: "float", nullable: false),
                    XgPerMatch = table.Column<double>(type: "float", nullable: false),
                    XgAgainstPerMatch = table.Column<double>(type: "float", nullable: false),
                    GoalsScored = table.Column<int>(type: "int", nullable: false),
                    GoalsConceded = table.Column<int>(type: "int", nullable: false),
                    XgOverperformance = table.Column<double>(type: "float", nullable: false),
                    XgaOverperformance = table.Column<double>(type: "float", nullable: false),
                    RecentXgPerMatch = table.Column<double>(type: "float", nullable: false),
                    RecentXgAgainstPerMatch = table.Column<double>(type: "float", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "int", nullable: false),
                    UnderstatTeamId = table.Column<int>(type: "int", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamXgStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamXgStats_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamXgStats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchXgStats_MatchId",
                table: "MatchXgStats",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchXgStats_UnderstatMatchId",
                table: "MatchXgStats",
                column: "UnderstatMatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamXgStats_CompetitionId_Season",
                table: "TeamXgStats",
                columns: new[] { "CompetitionId", "Season" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamXgStats_TeamId_CompetitionId_Season",
                table: "TeamXgStats",
                columns: new[] { "TeamId", "CompetitionId", "Season" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchXgStats");

            migrationBuilder.DropTable(
                name: "TeamXgStats");
        }
    }
}
