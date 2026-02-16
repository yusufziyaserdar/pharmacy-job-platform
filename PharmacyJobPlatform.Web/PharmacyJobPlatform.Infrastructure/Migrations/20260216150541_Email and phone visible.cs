using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyJobPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Emailandphonevisible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVisible",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneNumberVisible",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailVisible",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPhoneNumberVisible",
                table: "Users");
        }
    }
}
