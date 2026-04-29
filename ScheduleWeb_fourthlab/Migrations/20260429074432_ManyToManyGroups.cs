using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleWeb_fourthlab.Migrations
{
    /// <inheritdoc />
    public partial class ManyToManyGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_StudyGroups_StudyGroupId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_StudyGroupId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "StudyGroupId",
                table: "Lessons");

            migrationBuilder.CreateTable(
                name: "LessonStudyGroup",
                columns: table => new
                {
                    GroupsId = table.Column<int>(type: "int", nullable: false),
                    LessonsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonStudyGroup", x => new { x.GroupsId, x.LessonsId });
                    table.ForeignKey(
                        name: "FK_LessonStudyGroup_Lessons_LessonsId",
                        column: x => x.LessonsId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonStudyGroup_StudyGroups_GroupsId",
                        column: x => x.GroupsId,
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonStudyGroup_LessonsId",
                table: "LessonStudyGroup",
                column: "LessonsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonStudyGroup");

            migrationBuilder.AddColumn<int>(
                name: "StudyGroupId",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_StudyGroupId",
                table: "Lessons",
                column: "StudyGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_StudyGroups_StudyGroupId",
                table: "Lessons",
                column: "StudyGroupId",
                principalTable: "StudyGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
