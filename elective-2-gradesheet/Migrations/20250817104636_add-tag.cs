using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace elective_2_gradesheet.Migrations
{
    /// <inheritdoc />
    public partial class addtag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "Activities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tag",
                table: "Activities");
        }
    }
}
