using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonAndStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeasonId",
                table: "Matches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<int>(type: "int", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentMatchday = table.Column<int>(type: "int", nullable: false),
                    WinnerTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    TeamsLastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StandingsLastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seasons_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seasons_Teams_WinnerTeamId",
                        column: x => x.WinnerTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FootballStandings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Group = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PlayedGames = table.Column<int>(type: "int", nullable: false),
                    Won = table.Column<int>(type: "int", nullable: false),
                    Draw = table.Column<int>(type: "int", nullable: false),
                    Lost = table.Column<int>(type: "int", nullable: false),
                    GoalsFor = table.Column<int>(type: "int", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    GoalDifference = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Form = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FootballStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FootballStandings_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FootballStandings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonTeams_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO Seasons (Id, ExternalId, CompetitionId, StartYear, StartDate, EndDate, CurrentMatchday, IsCurrent, CreatedAt, UpdatedAt)
                SELECT NEWID(), 0, c.Id, YEAR(c.CurrentSeasonStart), c.CurrentSeasonStart, c.CurrentSeasonEnd,
                       ISNULL(c.CurrentMatchday, 1), 1, GETUTCDATE(), GETUTCDATE()
                FROM Competitions c WHERE c.CurrentSeasonStart IS NOT NULL;

                INSERT INTO SeasonTeams (Id, SeasonId, TeamId, CreatedAt, UpdatedAt)
                SELECT NEWID(), s.Id, ct.TeamId, GETUTCDATE(), GETUTCDATE()
                FROM CompetitionTeams ct
                INNER JOIN Seasons s ON s.CompetitionId = ct.CompetitionId AND s.StartYear = ct.Season;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_SeasonId",
                table: "Matches",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_FootballStandings_SeasonId_TeamId_Type_Stage_Group",
                table: "FootballStandings",
                columns: new[] { "SeasonId", "TeamId", "Type", "Stage", "Group" },
                unique: true,
                filter: "[Stage] IS NOT NULL AND [Group] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FootballStandings_SeasonId_Type_Position",
                table: "FootballStandings",
                columns: new[] { "SeasonId", "Type", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FootballStandings_TeamId",
                table: "FootballStandings",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_CompetitionId_ExternalId",
                table: "Seasons",
                columns: new[] { "CompetitionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_CompetitionId_IsCurrent",
                table: "Seasons",
                columns: new[] { "CompetitionId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_WinnerTeamId",
                table: "Seasons",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeams_SeasonId_TeamId",
                table: "SeasonTeams",
                columns: new[] { "SeasonId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeams_TeamId",
                table: "SeasonTeams",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Seasons_SeasonId",
                table: "Matches",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM SeasonTeams;
                DELETE FROM FootballStandings;
                DELETE FROM Seasons;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Seasons_SeasonId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "FootballStandings");

            migrationBuilder.DropTable(
                name: "SeasonTeams");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Matches_SeasonId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "Matches");
        }
    }
}
