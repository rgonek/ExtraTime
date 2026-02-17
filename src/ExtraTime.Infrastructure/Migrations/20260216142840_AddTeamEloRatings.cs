using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamEloRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamEloRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EloRating = table.Column<double>(type: "float", nullable: false),
                    EloRank = table.Column<int>(type: "int", nullable: false),
                    ClubEloName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RatingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamEloRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamEloRatings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamEloRatings_RatingDate",
                table: "TeamEloRatings",
                column: "RatingDate");

            migrationBuilder.CreateIndex(
                name: "IX_TeamEloRatings_TeamId_RatingDate",
                table: "TeamEloRatings",
                columns: new[] { "TeamId", "RatingDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamEloRatings");
        }
    }
}
