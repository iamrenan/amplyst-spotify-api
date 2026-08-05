using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amplyst_spotify_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyArtistId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyArtistUri = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, collation: "Latin1_General_CS_AS"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpotifyItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyItemUri = table.Column<string>(type: "nvarchar(450)", nullable: false, collation: "Latin1_General_CS_AS"),
                    IsLocal = table.Column<bool>(type: "bit", nullable: false),
                    AlbumName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscNumber = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    Explicit = table.Column<bool>(type: "bit", nullable: true),
                    ReleaseDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReleaseDatePrecision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ISRC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EAN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UPC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PlaylistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpotifyPlaylistId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyItemUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyAddedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyAddedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyPlaylistId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifySnapshotId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpotifyPlaylistUri = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SpotifyOwnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemArtists",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemArtists", x => new { x.ItemId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_ItemArtists_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemArtists_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_SpotifyArtistUri",
                table: "Artists",
                column: "SpotifyArtistUri",
                unique: true,
                filter: "[SpotifyArtistUri] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemArtists_ArtistId",
                table: "ItemArtists",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_SpotifyItemUri",
                table: "Items",
                column: "SpotifyItemUri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_ItemId",
                table: "PlaylistItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId_ItemId",
                table: "PlaylistItems",
                columns: new[] { "PlaylistId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_SpotifyPlaylistUri",
                table: "Playlists",
                column: "SpotifyPlaylistUri",
                unique: true,
                filter: "[SpotifyPlaylistUri] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "ItemArtists");

            migrationBuilder.DropTable(
                name: "PlaylistItems");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
