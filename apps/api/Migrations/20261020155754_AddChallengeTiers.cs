using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "challenge_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_level = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_tiers", x => x.id);
                    table.CheckConstraint("ck_challenge_tiers_min_level_positive", "min_level > 0");
                    table.CheckConstraint("ck_challenge_tiers_title_shape", "length(trim(title)) BETWEEN 1 AND 60");
                    table.ForeignKey(
                        name: "FK_challenge_tiers_challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "challenges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_challenge_tiers_challenge_min_level",
                table: "challenge_tiers",
                columns: new[] { "challenge_id", "min_level" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "challenge_tiers");
        }
    }
}
