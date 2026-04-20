using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedKernel.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Agency",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Agency",
                table: "Users");
        }
    }
}
