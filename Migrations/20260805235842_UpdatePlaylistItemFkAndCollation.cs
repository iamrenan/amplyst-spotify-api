using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace amplyst_spotify_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlaylistItemFkAndCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SpotifyPlaylistUri",
                table: "Playlists",
                type: "nvarchar(450)",
                nullable: true,
                collation: "Latin1_General_CS_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistItems_Items_ItemId",
                table: "PlaylistItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistItems_Playlists_PlaylistId",
                table: "PlaylistItems",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistItems_Items_ItemId",
                table: "PlaylistItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistItems_Playlists_PlaylistId",
                table: "PlaylistItems");

            migrationBuilder.AlterColumn<string>(
                name: "SpotifyPlaylistUri",
                table: "Playlists",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true,
                oldCollation: "Latin1_General_CS_AS");
        }
    }
}
