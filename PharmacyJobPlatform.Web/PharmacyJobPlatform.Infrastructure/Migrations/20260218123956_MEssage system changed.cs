using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyJobPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MEssagesystemchanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeletedByReceiver",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeletedBySender",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecalled",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecalledAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndedByUserId",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "User1Deleted",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "User2Deleted",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedByReceiver",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedBySender",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsRecalled",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "RecalledAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "EndedByUserId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User1Deleted",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User2Deleted",
                table: "Conversations");
        }
    }
}
