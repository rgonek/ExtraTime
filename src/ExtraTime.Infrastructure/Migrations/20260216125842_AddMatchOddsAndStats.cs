using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchOddsAndStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchOdds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HomeWinOdds = table.Column<double>(type: "float", nullable: false),
                    DrawOdds = table.Column<double>(type: "float", nullable: false),
                    AwayWinOdds = table.Column<double>(type: "float", nullable: false),
                    HomeWinProbability = table.Column<double>(type: "float", nullable: false),
                    DrawProbability = table.Column<double>(type: "float", nullable: false),
                    AwayWinProbability = table.Column<double>(type: "float", nullable: false),
                    Over25Odds = table.Column<double>(type: "float", nullable: true),
                    Under25Odds = table.Column<double>(type: "float", nullable: true),
                    BttsYesOdds = table.Column<double>(type: "float", nullable: true),
                    BttsNoOdds = table.Column<double>(type: "float", nullable: true),
                    MarketFavorite = table.Column<int>(type: "int", nullable: false),
                    FavoriteConfidence = table.Column<double>(type: "float", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchOdds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchOdds_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HomeShots = table.Column<int>(type: "int", nullable: true),
                    HomeShotsOnTarget = table.Column<int>(type: "int", nullable: true),
                    AwayShots = table.Column<int>(type: "int", nullable: true),
                    AwayShotsOnTarget = table.Column<int>(type: "int", nullable: true),
                    HomeHalfTimeGoals = table.Column<int>(type: "int", nullable: true),
                    AwayHalfTimeGoals = table.Column<int>(type: "int", nullable: true),
                    HomeCorners = table.Column<int>(type: "int", nullable: true),
                    AwayCorners = table.Column<int>(type: "int", nullable: true),
                    HomeFouls = table.Column<int>(type: "int", nullable: true),
                    AwayFouls = table.Column<int>(type: "int", nullable: true),
                    HomeYellowCards = table.Column<int>(type: "int", nullable: true),
                    AwayYellowCards = table.Column<int>(type: "int", nullable: true),
                    HomeRedCards = table.Column<int>(type: "int", nullable: true),
                    AwayRedCards = table.Column<int>(type: "int", nullable: true),
                    Referee = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStats_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchOdds_MatchId",
                table: "MatchOdds",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchStats_MatchId",
                table: "MatchStats",
                column: "MatchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchOdds");

            migrationBuilder.DropTable(
                name: "MatchStats");
        }
    }
}
