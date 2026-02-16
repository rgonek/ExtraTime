using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamInjuries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerInjuries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalPlayerId = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsKeyPlayer = table.Column<bool>(type: "bit", nullable: false),
                    InjuryType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InjurySeverity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InjuryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedReturn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDoubtful = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInjuries_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamInjuries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalInjured = table.Column<int>(type: "int", nullable: false),
                    KeyPlayersInjured = table.Column<int>(type: "int", nullable: false),
                    LongTermInjuries = table.Column<int>(type: "int", nullable: false),
                    ShortTermInjuries = table.Column<int>(type: "int", nullable: false),
                    Doubtful = table.Column<int>(type: "int", nullable: false),
                    InjuredPlayerNames = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TopScorerInjured = table.Column<bool>(type: "bit", nullable: false),
                    CaptainInjured = table.Column<bool>(type: "bit", nullable: false),
                    FirstChoiceGkInjured = table.Column<bool>(type: "bit", nullable: false),
                    InjuryImpactScore = table.Column<double>(type: "float", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextSyncDue = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamInjuries_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInjuries_TeamId_IsActive",
                table: "PlayerInjuries",
                columns: new[] { "TeamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInjuries_TeamId",
                table: "TeamInjuries",
                column: "TeamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerInjuries");

            migrationBuilder.DropTable(
                name: "TeamInjuries");
        }
    }
}
