using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScanlineBloomBackground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid,scanline_bloom")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid,scanline_bloom")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
