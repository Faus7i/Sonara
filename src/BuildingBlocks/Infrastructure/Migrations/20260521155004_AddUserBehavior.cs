using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBehaviorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPlayHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationPlayed = table.Column<int>(type: "int", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlayHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FavoriteGenres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvgEnergy = table.Column<double>(type: "float", nullable: false),
                    AvgDanceability = table.Column<double>(type: "float", nullable: false),
                    AvgValence = table.Column<double>(type: "float", nullable: false),
                    AvgTempo = table.Column<double>(type: "float", nullable: false),
                    AvgAcousticness = table.Column<double>(type: "float", nullable: false),
                    TopArtists = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TopTracks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExplorationLevel = table.Column<double>(type: "float", nullable: false),
                    TotalPlayCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_UserId_CreatedAt",
                table: "UserBehaviorEvents",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_UserId_EventType_CreatedAt",
                table: "UserBehaviorEvents",
                columns: new[] { "UserId", "EventType", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserPlayHistory_UserId_PlayedAt",
                table: "UserPlayHistory",
                columns: new[] { "UserId", "PlayedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserPlayHistory_UserId_TrackId_PlayedAt",
                table: "UserPlayHistory",
                columns: new[] { "UserId", "TrackId", "PlayedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBehaviorEvents");

            migrationBuilder.DropTable(
                name: "UserPlayHistory");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
