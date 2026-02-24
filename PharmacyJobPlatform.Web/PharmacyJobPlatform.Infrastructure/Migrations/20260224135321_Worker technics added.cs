using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyJobPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Workertechnicsadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CrossSellingSkillLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DermocosmeticKnowledgeLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DrugKnowledgeLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PharmacyPrograms",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrescriptionControlLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrescriptionPreparationLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportControlLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SutKnowledgeLevel",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrossSellingSkillLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DermocosmeticKnowledgeLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DrugKnowledgeLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PharmacyPrograms",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrescriptionControlLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrescriptionPreparationLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReportControlLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SutKnowledgeLevel",
                table: "Users");
        }
    }
}
