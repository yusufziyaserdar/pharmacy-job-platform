using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyJobPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Commentsystemadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfileComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileUserId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileComments_ProfileComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "ProfileComments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProfileComments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileComments_Users_ProfileUserId",
                        column: x => x.ProfileUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileComments_AuthorUserId",
                table: "ProfileComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileComments_ParentCommentId",
                table: "ProfileComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileComments_ProfileUserId",
                table: "ProfileComments",
                column: "ProfileUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileComments");
        }
    }
}
