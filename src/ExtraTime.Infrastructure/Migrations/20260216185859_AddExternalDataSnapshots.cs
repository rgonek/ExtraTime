using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalDataSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamInjurySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDateUtc = table.Column<DateTime>(type: "date", nullable: false),
                    TotalInjured = table.Column<int>(type: "int", nullable: false),
                    KeyPlayersInjured = table.Column<int>(type: "int", nullable: false),
                    InjuryImpactScore = table.Column<double>(type: "float", nullable: false),
                    InjuredPlayerNames = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamInjurySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamInjurySnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamXgSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SnapshotDateUtc = table.Column<DateTime>(type: "date", nullable: false),
                    XgPerMatch = table.Column<double>(type: "float", nullable: false),
                    XgAgainstPerMatch = table.Column<double>(type: "float", nullable: false),
                    XgOverperformance = table.Column<double>(type: "float", nullable: false),
                    RecentXgPerMatch = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamXgSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamXgSnapshots_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamXgSnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInjurySnapshots_TeamId_SnapshotDateUtc",
                table: "TeamInjurySnapshots",
                columns: new[] { "TeamId", "SnapshotDateUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamXgSnapshots_CompetitionId_Season_SnapshotDateUtc",
                table: "TeamXgSnapshots",
                columns: new[] { "CompetitionId", "Season", "SnapshotDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamXgSnapshots_TeamId_CompetitionId_Season_SnapshotDateUtc",
                table: "TeamXgSnapshots",
                columns: new[] { "TeamId", "CompetitionId", "Season", "SnapshotDateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamInjurySnapshots");

            migrationBuilder.DropTable(
                name: "TeamXgSnapshots");
        }
    }
}
