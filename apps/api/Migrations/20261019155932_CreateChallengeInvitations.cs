using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateChallengeInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "challenge_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitee_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_invitations", x => x.id);
                    table.CheckConstraint("ck_challenge_invitations_state", "state IN ('pending', 'accepted', 'rejected', 'expired')");
                    table.ForeignKey(
                        name: "FK_challenge_invitations_challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "challenges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_challenge_invitations_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_challenge_invitations_users_invitee_user_id",
                        column: x => x.invitee_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_challenge_invitations_challenge_invitee",
                table: "challenge_invitations",
                columns: new[] { "challenge_id", "invitee_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_challenge_invitations_challenge_state",
                table: "challenge_invitations",
                columns: new[] { "challenge_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_challenge_invitations_invited_by_user_id",
                table: "challenge_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_invitations_invitee_user_id",
                table: "challenge_invitations",
                column: "invitee_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "challenge_invitations");
        }
    }
}
