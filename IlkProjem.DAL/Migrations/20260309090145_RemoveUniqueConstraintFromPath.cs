using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IlkProjem.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintFromPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_RelativePath",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_Files_FileHash",
                table: "Files",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_Files_RelativePath",
                table: "Files",
                column: "RelativePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_FileHash",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_RelativePath",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_Files_RelativePath",
                table: "Files",
                column: "RelativePath",
                unique: true);
        }
    }
}
