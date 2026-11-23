using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInReactionsAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "check_in_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_comments", x => x.id);
                    table.CheckConstraint("ck_check_in_comments_body_length", "length(trim(body)) > 0 AND length(body) <= 280");
                    table.ForeignKey(
                        name: "FK_check_in_comments_check_ins_check_in_id",
                        column: x => x.check_in_id,
                        principalTable: "check_ins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_check_in_comments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "check_in_reactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emoji = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_reactions", x => x.id);
                    table.CheckConstraint("ck_check_in_reactions_emoji_not_blank", "length(trim(emoji)) > 0");
                    table.ForeignKey(
                        name: "FK_check_in_reactions_check_ins_check_in_id",
                        column: x => x.check_in_id,
                        principalTable: "check_ins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_check_in_reactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_check_in_comments_check_in_created_at",
                table: "check_in_comments",
                columns: new[] { "check_in_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_check_in_comments_live",
                table: "check_in_comments",
                column: "check_in_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_comments_user_id",
                table: "check_in_comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_in_reactions_check_in_id",
                table: "check_in_reactions",
                column: "check_in_id");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_reactions_user_id",
                table: "check_in_reactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_check_in_reactions_check_user_emoji",
                table: "check_in_reactions",
                columns: new[] { "check_in_id", "user_id", "emoji" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_comments");

            migrationBuilder.DropTable(
                name: "check_in_reactions");
        }
    }
}
