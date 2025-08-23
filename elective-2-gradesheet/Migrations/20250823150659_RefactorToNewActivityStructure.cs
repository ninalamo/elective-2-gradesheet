using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace elective_2_gradesheet.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToNewActivityStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities_New",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    SchoolYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Period = table.Column<int>(type: "int", nullable: false),
                    MaxPoints = table.Column<double>(type: "float", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RubricJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities_New", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_Activities_New_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rubrics",
                columns: table => new
                {
                    RubricId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RubricJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubrics", x => x.RubricId);
                    table.ForeignKey(
                        name: "FK_Rubrics_Activities_New_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities_New",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentActivities",
                columns: table => new
                {
                    StudentActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GithubLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GradedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RubricScoreJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentActivities", x => x.StudentActivityId);
                    table.ForeignKey(
                        name: "FK_StudentActivities_Activities_New_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities_New",
                        principalColumn: "ActivityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentActivities_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_New_SectionId",
                table: "Activities_New",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_NewActivity_Unique",
                table: "Activities_New",
                columns: new[] { "Name", "SectionId", "SchoolYear", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rubric_ActivityId",
                table: "Rubrics",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentActivities_ActivityId",
                table: "StudentActivities",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentActivity_Unique",
                table: "StudentActivities",
                columns: new[] { "StudentId", "ActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rubrics");

            migrationBuilder.DropTable(
                name: "StudentActivities");

            migrationBuilder.DropTable(
                name: "Activities_New");
        }
    }
}
