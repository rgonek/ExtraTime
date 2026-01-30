using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamFormCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamFormCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Draws = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    GoalsScored = table.Column<int>(type: "int", nullable: false),
                    GoalsConceded = table.Column<int>(type: "int", nullable: false),
                    HomeMatchesPlayed = table.Column<int>(type: "int", nullable: false),
                    HomeWins = table.Column<int>(type: "int", nullable: false),
                    HomeGoalsScored = table.Column<int>(type: "int", nullable: false),
                    HomeGoalsConceded = table.Column<int>(type: "int", nullable: false),
                    AwayMatchesPlayed = table.Column<int>(type: "int", nullable: false),
                    AwayWins = table.Column<int>(type: "int", nullable: false),
                    AwayGoalsScored = table.Column<int>(type: "int", nullable: false),
                    AwayGoalsConceded = table.Column<int>(type: "int", nullable: false),
                    PointsPerMatch = table.Column<double>(type: "float", nullable: false),
                    GoalsPerMatch = table.Column<double>(type: "float", nullable: false),
                    GoalsConcededPerMatch = table.Column<double>(type: "float", nullable: false),
                    HomeWinRate = table.Column<double>(type: "float", nullable: false),
                    AwayWinRate = table.Column<double>(type: "float", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    RecentForm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchesAnalyzed = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMatchDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamFormCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamFormCaches_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamFormCaches_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamFormCaches_CalculatedAt",
                table: "TeamFormCaches",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFormCaches_CompetitionId",
                table: "TeamFormCaches",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFormCaches_TeamId_CompetitionId",
                table: "TeamFormCaches",
                columns: new[] { "TeamId", "CompetitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamFormCaches");
        }
    }
}
