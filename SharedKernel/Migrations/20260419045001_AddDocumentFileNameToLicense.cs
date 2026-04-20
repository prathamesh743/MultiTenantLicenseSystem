using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedKernel.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentFileNameToLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "Licenses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "Licenses");
        }
    }
}
