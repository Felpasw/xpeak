using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_public_id = table.Column<string>(type: "text", nullable: true),
                    visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_level = table.Column<int>(type: "integer", nullable: false),
                    xp_multiplier = table.Column<decimal>(type: "numeric(4,2)", nullable: false, defaultValue: 1.0m),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    member_cap = table.Column<int>(type: "integer", nullable: true),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenges", x => x.id);
                    table.CheckConstraint("ck_challenges_member_cap_positive", "member_cap IS NULL OR member_cap > 0");
                    table.CheckConstraint("ck_challenges_state", "state IN ('active', 'completed', 'failed')");
                    table.CheckConstraint("ck_challenges_target_level_positive", "target_level > 0");
                    table.CheckConstraint("ck_challenges_title_shape", "length(trim(title)) > 0 AND length(title) <= 80");
                    table.CheckConstraint("ck_challenges_visibility", "visibility IN ('public', 'private')");
                    table.CheckConstraint("ck_challenges_window", "starts_at < ends_at");
                    table.CheckConstraint("ck_challenges_xp_multiplier_range", "xp_multiplier >= 0.5 AND xp_multiplier <= 1.5");
                    table.ForeignKey(
                        name: "FK_challenges_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "challenge_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "challenge_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    progress = table.Column<string>(type: "jsonb", nullable: false),
                    result = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_memberships", x => x.id);
                    table.CheckConstraint("ck_challenge_memberships_result", "result IS NULL OR result IN ('completed', 'failed', 'cancelled')");
                    table.CheckConstraint("ck_challenge_memberships_role", "role IN ('owner', 'member')");
                    table.ForeignKey(
                        name: "FK_challenge_memberships_challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "challenges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_challenge_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
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

            migrationBuilder.CreateIndex(
                name: "IX_challenge_memberships_user_id",
                table: "challenge_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_challenge_memberships_challenge_user",
                table: "challenge_memberships",
                columns: new[] { "challenge_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_challenges_owner_id",
                table: "challenges",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_challenges_state",
                table: "challenges",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_challenges_visibility",
                table: "challenges",
                column: "visibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "challenge_categories");

            migrationBuilder.DropTable(
                name: "challenge_memberships");

            migrationBuilder.DropTable(
                name: "challenges");
        }
    }
}
