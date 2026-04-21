using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedAlert.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    IsBatteryOptimized = table.Column<bool>(type: "boolean", nullable: false),
                    BackgroundLocationGranted = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadLookupCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CacheKey = table.Column<string>(type: "text", nullable: false),
                    RoadName = table.Column<string>(type: "text", nullable: false),
                    SpeedLimitKph = table.Column<double>(type: "double precision", nullable: false),
                    RetrievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadLookupCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalDistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    AverageSpeedKph = table.Column<double>(type: "double precision", nullable: false),
                    MaxSpeedKph = table.Column<double>(type: "double precision", nullable: false),
                    OverspeedEventCount = table.Column<int>(type: "integer", nullable: false),
                    AlertEventCount = table.Column<int>(type: "integer", nullable: false),
                    MostSevereOverspeedKph = table.Column<double>(type: "double precision", nullable: false),
                    SessionStartReason = table.Column<string>(type: "text", nullable: false),
                    SessionEndReason = table.Column<string>(type: "text", nullable: false),
                    WasAutoStarted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpeedUnit = table.Column<string>(type: "text", nullable: false),
                    OverspeedTolerance = table.Column<int>(type: "integer", nullable: false),
                    AlertDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    AlertCooldownSeconds = table.Column<int>(type: "integer", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    VibrationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    VoiceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoDetectDrivingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoStartMonitoringEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrivingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    ActualSpeedKph = table.Column<double>(type: "double precision", nullable: false),
                    SpeedLimitKph = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEvents_Sessions_DrivingSessionId",
                        column: x => x.DrivingSessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrivingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    SpeedKph = table.Column<double>(type: "double precision", nullable: false),
                    AccuracyMeters = table.Column<double>(type: "double precision", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionPoints_Sessions_DrivingSessionId",
                        column: x => x.DrivingSessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_DrivingSessionId",
                table: "AlertEvents",
                column: "DrivingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadLookupCaches_CacheKey",
                table: "RoadLookupCaches",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionPoints_DrivingSessionId",
                table: "SessionPoints",
                column: "DrivingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "DeviceStatuses");

            migrationBuilder.DropTable(
                name: "RoadLookupCaches");

            migrationBuilder.DropTable(
                name: "SessionPoints");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
