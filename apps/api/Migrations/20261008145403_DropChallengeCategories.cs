using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropChallengeCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "challenge_categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "challenge_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_challenge_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_challenge_categories_challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "challenges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_categories_category_id",
                table: "challenge_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ux_challenge_categories_challenge_category",
                table: "challenge_categories",
                columns: new[] { "challenge_id", "category_id" },
                unique: true);
        }
    }
}
