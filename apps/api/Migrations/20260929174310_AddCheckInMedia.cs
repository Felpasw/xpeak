using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "check_in_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_media", x => x.id);
                    table.CheckConstraint("ck_check_in_media_dimensions_non_negative", "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)");
                    table.CheckConstraint("ck_check_in_media_duration_non_negative", "duration_seconds IS NULL OR duration_seconds > 0");
                    table.CheckConstraint("ck_check_in_media_kind", "kind IN ('photo', 'video')");
                    table.CheckConstraint("ck_check_in_media_position_non_negative", "position >= 0");
                    table.CheckConstraint("ck_check_in_media_storage_key_not_blank", "length(trim(storage_key)) > 0");
                    table.ForeignKey(
                        name: "FK_check_in_media_check_ins_check_in_id",
                        column: x => x.check_in_id,
                        principalTable: "check_ins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_check_in_media_check_in_position",
                table: "check_in_media",
                columns: new[] { "check_in_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ux_check_in_media_storage_key",
                table: "check_in_media",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_media");
        }
    }
}
