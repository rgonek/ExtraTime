using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtraTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMlBotEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotPredictionAccuracies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalPredictions = table.Column<int>(type: "int", nullable: false),
                    ExactScores = table.Column<int>(type: "int", nullable: false),
                    CorrectResults = table.Column<int>(type: "int", nullable: false),
                    GoalsOffBy1 = table.Column<int>(type: "int", nullable: false),
                    GoalsOffBy2 = table.Column<int>(type: "int", nullable: false),
                    GoalsOffBy3Plus = table.Column<int>(type: "int", nullable: false),
                    ExactScoreAccuracy = table.Column<double>(type: "float", nullable: false),
                    CorrectResultAccuracy = table.Column<double>(type: "float", nullable: false),
                    Within1GoalAccuracy = table.Column<double>(type: "float", nullable: false),
                    MeanAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    RootMeanSquaredError = table.Column<double>(type: "float", nullable: false),
                    HomeScoreMAE = table.Column<double>(type: "float", nullable: false),
                    AwayScoreMAE = table.Column<double>(type: "float", nullable: false),
                    TotalPointsEarned = table.Column<double>(type: "float", nullable: false),
                    AvgPointsPerBet = table.Column<double>(type: "float", nullable: false),
                    BetsWon = table.Column<int>(type: "int", nullable: false),
                    BetsLost = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotPredictionAccuracies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotPredictionAccuracies_Bots_BotId",
                        column: x => x.BotId,
                        principalTable: "Bots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MlModelVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BlobPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrainedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrainingSamples = table.Column<int>(type: "int", nullable: false),
                    TrainingDataRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Rsquared = table.Column<double>(type: "float", nullable: false),
                    MeanAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    RootMeanSquaredError = table.Column<double>(type: "float", nullable: false),
                    MeanAbsolutePercentageError = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FeatureImportanceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlgorithmUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HyperparametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrossValidationMAE = table.Column<double>(type: "float", nullable: false),
                    CrossValidationStdDev = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlModelVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotPredictionAccuracies_BotId_PeriodType_PeriodStart",
                table: "BotPredictionAccuracies",
                columns: new[] { "BotId", "PeriodType", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_MlModelVersions_ModelType_IsActive",
                table: "MlModelVersions",
                columns: new[] { "ModelType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MlModelVersions_Version",
                table: "MlModelVersions",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotPredictionAccuracies");

            migrationBuilder.DropTable(
                name: "MlModelVersions");
        }
    }
}
