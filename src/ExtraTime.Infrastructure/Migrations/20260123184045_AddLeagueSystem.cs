using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    ScoreExactMatch = table.Column<int>(type: "integer", nullable: false),
                    ScoreCorrectResult = table.Column<int>(type: "integer", nullable: false),
                    BettingDeadlineMinutes = table.Column<int>(type: "integer", nullable: false),
                    AllowedCompetitionIds = table.Column<string>(type: "text", nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    InviteCodeExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leagues_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "league_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_league_members_leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_league_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_league_members_LeagueId_UserId",
                table: "league_members",
                columns: new[] { "LeagueId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_members_UserId",
                table: "league_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_InviteCode",
                table: "leagues",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_OwnerId",
                table: "leagues",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "league_members");

            migrationBuilder.DropTable(
                name: "leagues");
        }
    }
}
