using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedAlert.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenProviderAndTrackingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_SessionPoints_DrivingSessionId",
                table: "SessionPoints");

            migrationBuilder.DropIndex(
                name: "IX_RoadLookupCaches_CacheKey",
                table: "RoadLookupCaches");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<double>(
                name: "SpeedLimitKph",
                table: "RoadLookupCaches",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<string>(
                name: "RoadName",
                table: "RoadLookupCaches",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "RoadLookupCaches",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "FallbackUsed",
                table: "RoadLookupCaches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LookupStatus",
                table: "RoadLookupCaches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderUsedKey",
                table: "RoadLookupCaches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SegmentIdentifier",
                table: "RoadLookupCaches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedProviderKey",
                table: "RoadLookupCaches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "RoadLookupCaches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailureAt",
                table: "ProviderConfigs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFailureReason",
                table: "ProviderConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastHealthStatus",
                table: "ProviderConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSuccessAt",
                table: "ProviderConfigs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_Status",
                table: "Sessions",
                columns: new[] { "UserId", "Status" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPoints_DrivingSessionId_Timestamp",
                table: "SessionPoints",
                columns: new[] { "DrivingSessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadLookupCaches_CacheKey_SelectedProviderKey",
                table: "RoadLookupCaches",
                columns: new[] { "CacheKey", "SelectedProviderKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_UserId_Status",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_SessionPoints_DrivingSessionId_Timestamp",
                table: "SessionPoints");

            migrationBuilder.DropIndex(
                name: "IX_RoadLookupCaches_CacheKey_SelectedProviderKey",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "FallbackUsed",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "LookupStatus",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "ProviderUsedKey",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "SegmentIdentifier",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "SelectedProviderKey",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "RoadLookupCaches");

            migrationBuilder.DropColumn(
                name: "LastFailureAt",
                table: "ProviderConfigs");

            migrationBuilder.DropColumn(
                name: "LastFailureReason",
                table: "ProviderConfigs");

            migrationBuilder.DropColumn(
                name: "LastHealthStatus",
                table: "ProviderConfigs");

            migrationBuilder.DropColumn(
                name: "LastSuccessAt",
                table: "ProviderConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<double>(
                name: "SpeedLimitKph",
                table: "RoadLookupCaches",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RoadName",
                table: "RoadLookupCaches",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPoints_DrivingSessionId",
                table: "SessionPoints",
                column: "DrivingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadLookupCaches_CacheKey",
                table: "RoadLookupCaches",
                column: "CacheKey",
                unique: true);
        }
    }
}
