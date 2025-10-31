using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace elective_2_gradesheet.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToActivityTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubmissions_ActivityTemplates_ActivityTemplateId",
                table: "StudentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ActivityTemplates_SectionId",
                table: "ActivityTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTemplates_SectionId_Period_Name",
                table: "ActivityTemplates",
                columns: new[] { "SectionId", "Period", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubmissions_ActivityTemplates_ActivityTemplateId",
                table: "StudentSubmissions",
                column: "ActivityTemplateId",
                principalTable: "ActivityTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSubmissions_ActivityTemplates_ActivityTemplateId",
                table: "StudentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ActivityTemplates_SectionId_Period_Name",
                table: "ActivityTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTemplates_SectionId",
                table: "ActivityTemplates",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSubmissions_ActivityTemplates_ActivityTemplateId",
                table: "StudentSubmissions",
                column: "ActivityTemplateId",
                principalTable: "ActivityTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
