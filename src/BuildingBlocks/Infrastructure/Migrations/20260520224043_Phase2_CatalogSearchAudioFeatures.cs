using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_CatalogSearchAudioFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpotifyAlbumId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReleaseDate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AlbumType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TotalTracks = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpotifyArtistId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Genres = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Popularity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackAudioFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpotifyTrackId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Acousticness = table.Column<float>(type: "real", nullable: false),
                    Danceability = table.Column<float>(type: "real", nullable: false),
                    Energy = table.Column<float>(type: "real", nullable: false),
                    Instrumentalness = table.Column<float>(type: "real", nullable: false),
                    Key = table.Column<int>(type: "int", nullable: false),
                    Liveness = table.Column<float>(type: "real", nullable: false),
                    Loudness = table.Column<float>(type: "real", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Speechiness = table.Column<float>(type: "real", nullable: false),
                    Tempo = table.Column<float>(type: "real", nullable: false),
                    TimeSignature = table.Column<int>(type: "int", nullable: false),
                    Valence = table.Column<float>(type: "real", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackAudioFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSearchHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSearchHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpotifyTrackId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AlbumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    ReleaseDate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackArtists",
                columns: table => new
                {
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackArtists", x => new { x.TrackId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_TrackArtists_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackArtists_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackGenres",
                columns: table => new
                {
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GenreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackGenres", x => new { x.TrackId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_TrackGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackGenres_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Albums_SpotifyAlbumId",
                table: "Albums",
                column: "SpotifyAlbumId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_SpotifyArtistId",
                table: "Artists",
                column: "SpotifyArtistId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackArtists_ArtistId",
                table: "TrackArtists",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackAudioFeatures_SpotifyTrackId",
                table: "TrackAudioFeatures",
                column: "SpotifyTrackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackGenres_GenreId",
                table: "TrackGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_AlbumId",
                table: "Tracks",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_SpotifyTrackId",
                table: "Tracks",
                column: "SpotifyTrackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSearchHistory_SearchedAt",
                table: "UserSearchHistory",
                column: "SearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSearchHistory_UserId",
                table: "UserSearchHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackArtists");

            migrationBuilder.DropTable(
                name: "TrackAudioFeatures");

            migrationBuilder.DropTable(
                name: "TrackGenres");

            migrationBuilder.DropTable(
                name: "UserSearchHistory");

            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "Albums");
        }
    }
}
