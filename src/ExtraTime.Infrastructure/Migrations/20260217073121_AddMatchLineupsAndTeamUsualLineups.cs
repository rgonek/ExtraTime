using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchLineupsAndTeamUsualLineups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchLineups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Formation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CoachName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartingXi = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Bench = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CaptainName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamUsualLineups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsualFormation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UsualGoalkeepers = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UsualDefenders = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UsualMidfielders = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UsualForwards = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CaptainName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MatchesAnalyzed = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamUsualLineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamUsualLineups_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamUsualLineups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_TeamId",
                table: "MatchLineups",
                columns: new[] { "MatchId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_TeamId",
                table: "MatchLineups",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUsualLineups_SeasonId",
                table: "TeamUsualLineups",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUsualLineups_TeamId_SeasonId",
                table: "TeamUsualLineups",
                columns: new[] { "TeamId", "SeasonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchLineups");

            migrationBuilder.DropTable(
                name: "TeamUsualLineups");
        }
    }
}
