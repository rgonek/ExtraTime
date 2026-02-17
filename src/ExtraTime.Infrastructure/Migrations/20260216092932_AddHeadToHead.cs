using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHeadToHead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeadToHeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalMatches = table.Column<int>(type: "int", nullable: false),
                    Team1Wins = table.Column<int>(type: "int", nullable: false),
                    Team2Wins = table.Column<int>(type: "int", nullable: false),
                    Draws = table.Column<int>(type: "int", nullable: false),
                    Team1Goals = table.Column<int>(type: "int", nullable: false),
                    Team2Goals = table.Column<int>(type: "int", nullable: false),
                    BothTeamsScoredCount = table.Column<int>(type: "int", nullable: false),
                    Over25Count = table.Column<int>(type: "int", nullable: false),
                    Team1HomeMatches = table.Column<int>(type: "int", nullable: false),
                    Team1HomeWins = table.Column<int>(type: "int", nullable: false),
                    Team1HomeGoals = table.Column<int>(type: "int", nullable: false),
                    Team1HomeConceded = table.Column<int>(type: "int", nullable: false),
                    LastMatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecentMatchesCount = table.Column<int>(type: "int", nullable: false),
                    RecentTeam1Wins = table.Column<int>(type: "int", nullable: false),
                    RecentTeam2Wins = table.Column<int>(type: "int", nullable: false),
                    RecentDraws = table.Column<int>(type: "int", nullable: false),
                    MatchesAnalyzed = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeadToHeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadToHeads_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HeadToHeads_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeadToHeads_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeadToHeads_CompetitionId",
                table: "HeadToHeads",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadToHeads_Team1Id_Team2Id_CompetitionId",
                table: "HeadToHeads",
                columns: new[] { "Team1Id", "Team2Id", "CompetitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeadToHeads_Team2Id",
                table: "HeadToHeads",
                column: "Team2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeadToHeads");
        }
    }
}
