using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeTierImageStorageKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_storage_key",
                table: "challenge_tiers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_storage_key",
                table: "challenge_tiers");
        }
    }
}
