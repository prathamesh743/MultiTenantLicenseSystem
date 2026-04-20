using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedKernel.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Licenses");
        }
    }
}
