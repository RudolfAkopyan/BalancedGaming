using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balanced_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamAppId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    gameId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    gameName = table.Column<string>(type: "TEXT", nullable: false),
                    processName = table.Column<string>(type: "TEXT", nullable: false),
                    genre = table.Column<string>(type: "TEXT", nullable: false),
                    addedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    isTracked = table.Column<bool>(type: "INTEGER", nullable: false),
                    isManuallyAdded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.gameId);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    defaultBreakRemindersMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    breakRemindersEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    moodTrackingEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.userId);
                });

            migrationBuilder.CreateTable(
                name: "gameSessions",
                columns: table => new
                {
                    sessionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    userId = table.Column<int>(type: "INTEGER", nullable: false),
                    gameId = table.Column<int>(type: "INTEGER", nullable: false),
                    startTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    endTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    breakRemindersShown = table.Column<int>(type: "INTEGER", nullable: false),
                    breakRemindersDismissed = table.Column<int>(type: "INTEGER", nullable: false),
                    breakTaken = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gameSessions", x => x.sessionId);
                    table.ForeignKey(
                        name: "FK_gameSessions_games_gameId",
                        column: x => x.gameId,
                        principalTable: "games",
                        principalColumn: "gameId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gameSessions_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moodAssessments",
                columns: table => new
                {
                    assessmentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    sessionId = table.Column<int>(type: "INTEGER", nullable: true),
                    userId = table.Column<int>(type: "INTEGER", nullable: false),
                    assessmentTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    moodScore = table.Column<int>(type: "INTEGER", nullable: false),
                    stressLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    energyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    motivation = table.Column<string>(type: "TEXT", nullable: true),
                    focusLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    satisfaction = table.Column<int>(type: "INTEGER", nullable: true),
                    wellbeingImpact = table.Column<int>(type: "INTEGER", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moodAssessments", x => x.assessmentId);
                    table.ForeignKey(
                        name: "FK_moodAssessments_gameSessions_sessionId",
                        column: x => x.sessionId,
                        principalTable: "gameSessions",
                        principalColumn: "sessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_moodAssessments_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gameSessions_gameId",
                table: "gameSessions",
                column: "gameId");

            migrationBuilder.CreateIndex(
                name: "IX_gameSessions_userId",
                table: "gameSessions",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_moodAssessments_sessionId",
                table: "moodAssessments",
                column: "sessionId");

            migrationBuilder.CreateIndex(
                name: "IX_moodAssessments_userId",
                table: "moodAssessments",
                column: "userId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moodAssessments");

            migrationBuilder.DropTable(
                name: "gameSessions");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
