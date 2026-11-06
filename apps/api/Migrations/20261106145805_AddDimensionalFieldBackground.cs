using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xpeak.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDimensionalFieldBackground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid,scanline_bloom,dimensional_field")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid,scanline_bloom")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid,scanline_bloom")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:backgrounds", "stars,lightning,etched_accretion,matrix,fluid,scanline_bloom,dimensional_field")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
