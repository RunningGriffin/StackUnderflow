using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassMate.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSemesterYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Semester",
                table: "Courses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Courses");
        }
    }
}
