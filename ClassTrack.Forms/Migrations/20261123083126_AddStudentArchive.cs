using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Students",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Students");
        }
    }
}
